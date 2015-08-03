﻿using NetMud.DataStructure.Base.System;
using NetMud.DataStructure.Behaviors.Rendering;
using NutMud.Commands.Attributes;
using System.Collections.Generic;

using NetMud.Utility;
using NetMud.DataStructure.Base.EntityBackingData;
using NetMud.DataStructure.SupportingClasses;
using NetMud.Data.EntityBackingData;
using NetMud.Data.Game;

namespace NutMud.Commands.System
{
    /// <summary>
    /// Spawns a new inanimate into the world.  Missing target parameter = container you're standing in
    /// </summary>
    [CommandKeyword("SpawnNewObject", false)]
    [CommandPermission(StaffRank.Admin)]
    [CommandParameter(CommandUsage.Subject, typeof(InanimateData), new CacheReferenceType[] { CacheReferenceType.Data }, "[0-9]+", false)] //for IDs
    [CommandParameter(CommandUsage.Subject, typeof(InanimateData), new CacheReferenceType[] { CacheReferenceType.Data }, "[a-zA-z]+", false)] //for names
    [CommandParameter(CommandUsage.Target, typeof(IContains), new CacheReferenceType[] { CacheReferenceType.Entity }, true)]
    [CommandRange(CommandRangeType.Touch, 0)]
    public class SpawnNewObject : ICommand, IHelpful
    {
        /// <summary>
        /// The entity invoking the command
        /// </summary>
        public IActor Actor { get; set; }

        /// <summary>
        /// The entity the command refers to
        /// </summary>
        public object Subject { get; set; }

        /// <summary>
        /// When there is a predicate parameter, the entity that is being targetting (subject become "with")
        /// </summary>
        public object Target { get; set; }

        /// <summary>
        /// Any tertiary entity being referenced in command parameters
        /// </summary>
        public object Supporting { get; set; }

        /// <summary>
        /// Container the Actor is in when the command is invoked
        /// </summary>
        public ILocation OriginLocation { get; set; }

        /// <summary>
        /// Valid containers by range from OriginLocation
        /// </summary>
        public IEnumerable<ILocation> Surroundings { get; set; }

        /// <summary>
        /// All Commands require a generic constructor
        /// </summary>
        public SpawnNewObject()
        {
            //Generic constructor for all IHelpfuls is needed
        }

        /// <summary>
        /// Executes this command
        /// </summary>
        public void Execute()
        {
            var newObject = (IInanimateData)Subject;
            var sb = new List<string>();
            IContains spawnTo;

            //No target = spawn to room you're in
            if (Target != null)
                spawnTo = (IContains)Target;
            else
                spawnTo = OriginLocation;

            var entityObject = new Inanimate(newObject, spawnTo);

            //TODO: keywords is janky, location should have its own identifier name somehow for output purposes
            sb.Add(string.Format("{0} spawned to {1}", entityObject.DataTemplate.Name, spawnTo.Keywords[0]));

            var messagingObject = new MessageCluster(RenderUtility.EncapsulateOutput(sb), "You are ALIVE", "You have been given $S$", "$S$ appears in the $T$.", string.Empty);

            messagingObject.ExecuteMessaging(Actor, entityObject, spawnTo, OriginLocation, null);
        }

        /// <summary>
        /// Renders syntactical help for the command, invokes automatically when syntax is bungled
        /// </summary>
        /// <returns>string</returns>
        public IEnumerable<string> RenderSyntaxHelp()
        {
            var sb = new List<string>();

            sb.Add(string.Format("Valid Syntax: spawnNewObject &lt;object name&gt;"));
            sb.Add("spawnNewObject  &lt;object name&gt;  &lt;location name to spawn to&gt;".PadWithString(14, "&nbsp;", true));

            return sb;
        }

        /// <summary>
        /// Renders the help text
        /// </summary>
        /// <returns>string</returns>
        public IEnumerable<string> RenderHelpBody()
        {
            var sb = new List<string>();

            sb.Add(string.Format("SpawnNewObject spawns a new object from its data template into the room or into a specified inventory."));

            return sb;
        }
    }
}
