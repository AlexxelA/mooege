﻿/*
 * Copyright (C) 2011 mooege project
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mooege.Net.GS.Message.Definitions.Conversation;
using Mooege.Net.GS.Message.Fields;
using Mooege.Net.GS.Message;
using Mooege.Net.GS;
using Mooege.Common.MPQ.FileFormats;
using Mooege.Common;
using Mooege.Common.Helpers;
using Mooege.Net.GS.Message.Definitions.ACD;
using Mooege.Core.GS.Common.Types.Math;
using Mooege.Core.GS.Games;

/*
 * a few notes to the poor guy who wants to improve the conversation system:
 * From my understanding, a conversation consists of a sequence of lines in RootTreeNodes
 * Each line has its own tree with different node types (I0 / first field in ConversationTreeNode)
 * 
 * - A node type 3 indicates this node has the lineID (for the client), the speaker enum, and an animation tag.
 * - Each node of type 3 has nodes of type 5, thats hold information on how long the audio goes (for starting the next line)
 *   depending on class and gender of the player, even if that part is not actually spoken by the player actor but another.
 *   If the speaker of it parent node is the player, then there may be as many as five nodes (on for each class) with only
 *   the ConvLocalDisplayTime for that class but sometimes, there is not a node for each class but a "for all" node with all information combined
 * - A node type of 4 indicates that it children are alternatives. So i pick a random one
 * 
 * NodeType4          1. Line, pick a random child
 *   NodeType3        1. Line, 1. alternative. LineID and AnimationTag
 *     NodeType5 3    1. Line, 1. alternative, duration for some class
 *     NodeType5 4    1. Line, 1. alternative, duration for another class
 *     NodeType5 -1   1. Line, 1. alternative, duration for all classes (may not always be there, renders other node5 useless/redundant)
 *   NodeType3        1. Line, 2. alternative, LineID and AnimationTag
 *     NodeType5      1. Line, 2. alternative, duration for all classes (may not always be there, renders other node5 useless/redundant)
 * NodeType3          2. Line, LineID and AnimationTag
 *   NodeType5        2. Line, duration (-1, npc speaking, not dependant on player class choice
 * 
 * There is at least also a NodeType6 but if have no clue what it does
 * 
 * ConvLocalDisplayTimes is an array with another array for every supported language
 * The second array holds information for the audio duration of different speakers in game ticks
 * [BarbarianMale, BarbarianFemale, DemonHunterMale, ...]
 * 
 * good luck :-) - farmy
 */
namespace Mooege.Core.GS.Players
{
    /// <summary>
    /// Wraps a conversation asset and manages the whole conversation
    /// </summary>
    class Conversation
    {
        Logger logger = new Logger("Conversation");
        public event EventHandler ConversationEnded;

        public int ConvPiggyBack { get { return asset.SNOConvPiggyback; } }
        public int SNOId { get { return asset.Header.SNOId; } }

        private Mooege.Common.MPQ.FileFormats.Conversation asset;
        private int LineIndex = 0;              // index within the RootTreeNodes, conversation progress
        private Player player;
        private ConversationManager manager;
        private int currentUniqueLineID;        // id used to identify the current line clientside
        private int startTick = 0;              // start tick of the current line. used to determine, when to start the next line
        private ConversationTreeNode currentLineNode = null;

        // Find a childnode with a matching class id, that one holds information about how long the speaker talks
        // If there is no matching childnode, there must be one with -1 which only combines all class specific into one
        private int duration
        {
            get
            {
                var node = from a in currentLineNode.ChildNodes where a.ClassFilter == player.Toon.VoiceClassID select a;
                if (node.Count() == 0)
                    node = from a in currentLineNode.ChildNodes where a.ClassFilter == -1 select a;

                return node.First().ConvLocalDisplayTimes.ElementAt((int)manager.ClientLanguage).Languages[player.Toon.VoiceClassID * 2 + (player.Toon.Gender == 0 ? 0 : 1)];
            }
        }

        // This returns the dynamicID of other conversation partners. The client uses its position to identify where you can hear the conversation.
        // This implementation relies on there beeing exactly one actor with a given sno in the world!!
        // TODO Find a better way to get the dynamicID of actors or verify that this implementation is sound.
        // TODO add actor identification for Followers
        private Mooege.Core.GS.Actors.Actor GetSpeaker(Speaker speaker)
        {
            switch (speaker)
            {
                case Speaker.AltNPC1: return GetActorBySNO(asset.SNOAltNpc1);
                case Speaker.AltNPC2: return GetActorBySNO(asset.SNOAltNpc2);
                case Speaker.AltNPC3: return GetActorBySNO(asset.SNOAltNpc3);
                case Speaker.AltNPC4: return GetActorBySNO(asset.SNOAltNpc4);
                case Speaker.Player: return player;
                case Speaker.PrimaryNPC: return GetActorBySNO(asset.SNOPrimaryNpc);
                case Speaker.EnchantressFollower: return null;
                case Speaker.ScoundrelFollower: return null;
                case Speaker.TemplarFollower: return null;
                case Speaker.None: return null;
            }
            return null;
        }

        private Mooege.Core.GS.Actors.Actor GetActorBySNO(int sno)
        {
            var actors = (from a in player.RevealedObjects.Values where a is Mooege.Core.GS.Actors.Actor && (a as Mooege.Core.GS.Actors.Actor).SNOId == sno select a);
            if (actors.Count() > 1)
                logger.Warn("Found more than one actors in range");
            if (actors.Count() == 0)
            {
                logger.Warn("Actor not found, using player actor instead");
                return player;
            }
            return actors.First() as Mooege.Core.GS.Actors.Actor;
        }

        /// <summary>
        /// Creates a new conversation wrapper for an asset with a given sno.
        /// </summary>
        /// <param name="snoConversation">sno of the asset to wrap</param>
        /// <param name="player">player that receives messages</param>
        /// <param name="manager">the quest manager that provides ids</param>
        public Conversation(int snoConversation, Player player, ConversationManager manager)
        {
            asset = Mooege.Common.MPQ.MPQStorage.Data.Assets[Common.Types.SNO.SNOGroup.Conversation][snoConversation].Data as Mooege.Common.MPQ.FileFormats.Conversation;
            this.player = player;
            this.manager = manager;
        }

        /// <summary>
        /// Starts the conversation
        /// </summary>
        public void Start()
        {
            PlayLine(LineIndex);
        }

        /// <summary>
        /// Skips to the next line of the conversation
        /// </summary>
        public void Interrupt()
        {
            PlayNextLine(true);
        }

        /// <summary>
        /// Periodically call this method to make sure conversation progresses
        /// </summary>
        public void Update(int tickCounter)
        {
            if (startTick > 0 && currentLineNode == null)
                PlayNextLine(false);
            else
            {

                // rotate the primary speaker to face the secondary speaker
                if (currentLineNode.Speaker1 != Speaker.Player && currentLineNode.Speaker2 != Speaker.None)
                {
                    Mooege.Core.GS.Actors.Actor speaker1 = GetSpeaker(currentLineNode.Speaker1);
                    Mooege.Core.GS.Actors.Actor speaker2 = GetSpeaker(currentLineNode.Speaker2);
                    
                    Vector3D translation = speaker2.Position - speaker1.Position;
                    Vector2F flatTranslation = new Vector2F(translation.X, translation.Y);

                    speaker1.RotationAmount = flatTranslation.Rotation();

                    player.World.BroadcastIfRevealed(new ACDTranslateFacingMessage
                    {
                        Id = (int)Opcodes.ACDTranslateFacingMessage1,
                        ActorId = speaker1.DynamicID,
                        Angle = speaker1.RotationAmount,
                        Immediately = false
                    }, speaker1);
                }

                // start the next line if the playback has finished
                if (startTick + duration < tickCounter)
                    PlayNextLine(false);
            }
        }

        /// <summary>
        /// Stops current line and starts the next if there is one, or ends the conversation
        /// </summary>
        /// <param name="interrupt">sets, whether the speaker is interrupted</param>
        private void PlayNextLine(bool interrupt)
        {
            StopLine(interrupt);

            if (asset.RootTreeNodes.Count > LineIndex + 1)
                PlayLine(++LineIndex);
            else
                EndConversation();
        }

        /// <summary>
        /// Ends the conversation, though i dont know, what it actually does. This is only through observation
        /// </summary>
        private void EndConversation()
        {
            player.InGameClient.SendMessage(new EndConversationMessage()
            {
                Field0 = currentUniqueLineID,
                SNOConversation = asset.Header.SNOId,
                ActorId = player.DynamicID
            });

            player.InGameClient.SendMessage(new FinishConversationMessage
            {
                SNOConversation = asset.Header.SNOId
            });

            if (ConversationEnded != null)
                ConversationEnded(this, null);
        }

        /// <summary>
        /// Stops readout and display of current conversation line
        /// </summary>
        /// <param name="interrupted">sets whether the speaker is interrupted or not</param>
        private void StopLine(bool interrupted)
        {
            player.InGameClient.SendMessage(new StopConvLineMessage()
            {
                Field0 = currentUniqueLineID,
                Interrupt = interrupted,
            });
        }

        /// <summary>
        /// Starts readout and display of a certain conversation line
        /// </summary>
        /// <param name="LineIndex">index of the line withing the rootnodes</param>
        private void PlayLine(int LineIndex)
        {
            if (asset.RootTreeNodes[LineIndex].I0 == 6)
            {
                // dont know what to do with them yet, this just ignores them -farmy
                currentLineNode = null;
                return;
            }

            if (asset.RootTreeNodes[LineIndex].I0 == 4)
                currentLineNode = asset.RootTreeNodes[LineIndex].ChildNodes[RandomHelper.Next(asset.RootTreeNodes[LineIndex].ChildNodes.Count)];
            else
                currentLineNode = asset.RootTreeNodes[LineIndex];

            currentUniqueLineID = manager.GetNextUniqueLineID();
            startTick = player.World.Game.TickCounter;

            // TODO Actor id should be CurrentSpeaker.DynamicID not PrimaryNPC.ActorID. This is a workaround because no audio for the player is playing otherwise
            player.InGameClient.SendMessage(new PlayConvLineMessage()
            {
                ActorID = GetActorBySNO(asset.SNOPrimaryNpc).DynamicID, //CurrentSpeaker.DynamicID
                Field1 = new uint[9]
                        {
                            player.DynamicID, asset.SNOPrimaryNpc != -1 ? GetActorBySNO(asset.SNOPrimaryNpc).DynamicID : 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF, 0xFFFFFFFF
                        },

                Params = new PlayLineParams()
                {
                    SNOConversation = asset.Header.SNOId,
                    Field1 = 0x00000000,
                    Field2 = false,
                    LineID = currentLineNode.LineID,
                    Speaker = (int)currentLineNode.Speaker1,
                    Field5 = -1,
                    TextClass = currentLineNode.Speaker1 == Speaker.Player ? (Class)player.Toon.VoiceClassID : Class.None,
                    Gender = (player.Toon.Gender == 0) ? VoiceGender.Male : VoiceGender.Female,
                    AudioClass = (Class)player.Toon.VoiceClassID,
                    SNOSpeakerActor = GetSpeaker(currentLineNode.Speaker1).SNOId,
                    Name = player.Toon.Name,
                    Field11 = 0x00000000,  // is this field I1? and if...what does it do?? 2 for level up -farmy
                    AnimationTag = currentLineNode.AnimationTag,
                    Duration = duration,
                    Field14 = currentUniqueLineID,
                    Field15 = 0x00000000        // dont know, 0x32 for level up
                },
                Duration = duration,
            }, true);
        }
    }

    /// <summary>
    /// Manages conversations. Since you can (maybe?) only have one conversation at a time, this class may be merged with the player.
    /// Still, its a bit nicer this ways, considering the player class already has 1000+ loc
    /// </summary>
    public class ConversationManager
    {
        Logger logger = new Logger("ConversationManager");
        internal enum Language { Invalid, Global, enUS, enGB, enSG, esES, esMX, frFR, itIT, deDE, koKR, ptBR, ruRU, zhCN, zTW, trTR, plPL, ptPT }

        private Player player;
        private Dictionary<int, Conversation> openConversations = new Dictionary<int, Conversation>();
        private int linesPlayedTotal = 0;
        private QuestProgressHandler quests;

        internal Language ClientLanguage { get { return Language.enUS; } }

        internal int GetNextUniqueLineID()
        {
            return linesPlayedTotal++;
        }

        public ConversationManager(Player player, QuestProgressHandler quests)
        {
            this.player = player;
            this.quests = quests;
        }

        /// <summary>
        /// Starts and plays a conversation
        /// </summary>
        /// <param name="snoConversation">SnoID of the conversation</param>
        public void StartConversation(int snoConversation)
        {
            if (!Mooege.Common.MPQ.MPQStorage.Data.Assets[Common.Types.SNO.SNOGroup.Conversation].ContainsKey(snoConversation))
            {
                logger.Warn("Conversation not found: {0}", snoConversation);
                return;
            }

            if (!openConversations.ContainsKey(snoConversation))
            {
                Conversation newConversation = new Conversation(snoConversation, player, this);
                newConversation.Start();
                newConversation.ConversationEnded += new EventHandler(ConversationEnded);

                lock (openConversations)
                {
                    openConversations.Add(snoConversation, newConversation);

                }
            }
        }

        /// <summary>
        /// Remove conversation from the list of open conversations and start its piggyback conversation
        /// </summary>
        void ConversationEnded(object sender, EventArgs e)
        {
            Conversation conversation = sender as Conversation;
            quests.Notify(QuestStepObjectiveType.HadConversation, conversation.SNOId);

            lock (openConversations)
            {
                openConversations.Remove(conversation.SNOId);
            }

            if (conversation.ConvPiggyBack != -1)
                StartConversation(conversation.ConvPiggyBack);
        }

        /// <summary>
        /// Update all open conversations
        /// </summary>
        /// <param name="gameTick"></param>
        public void Update(int tickCounter)
        {
            List<Conversation> clonedList;

            // update from a cloned list, so you can remove conversations in their ConversationEnded event
            lock (openConversations)
            {
                clonedList = (from c in openConversations select c.Value).ToList();
            }

            foreach (var conversation in clonedList)
                conversation.Update(tickCounter);
        }

        /// <summary>
        /// Consumes conversations related messages
        /// </summary>
        /// <param name="client"></param>
        /// <param name="message"></param>
        public void Consume(GameClient client, GameMessage message)
        {
            lock (openConversations)
            {
                if (message is RequestCloseConversationWindowMessage)
                {
                    List<Conversation> clonedList;

                    lock (openConversations)
                    {
                        clonedList = (from c in openConversations select c.Value).ToList();
                    }

                    foreach (var conversation in clonedList)
                        conversation.Interrupt();
                }
            }
        }
    }
}