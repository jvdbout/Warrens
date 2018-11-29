﻿using NetMud.DataStructure.Base.System;
using System;
using System.Collections.Generic;

namespace NetMud.DataStructure.Base.PlayerConfiguration
{
    /// <summary>
    /// The account configuration for a player
    /// </summary>
    public interface IAccountConfig : IConfigData
    {
        /// <summary>
        /// What account owns this config
        /// </summary>
        IAccount Account { get; }

        /// <summary>
        /// Whether or not the person wants the tutorial tooltips on; false = off
        /// </summary>
        bool UITutorialMode { get; set; }

        /// <summary>
        /// Does someone see chatter from the Gossip network?
        /// </summary>
        bool GossipSubscriber { get; set; }

        /// <summary>
        /// The modules to load. Module, quadrant
        /// </summary>
        IEnumerable<Tuple<IUIModule, int>> UIModules { get; set; }

        /// <summary>
        /// Friends and Foes of this account
        /// </summary>
        IEnumerable<IAcquaintence> Acquaintences { get; set; }

        /// <summary>
        /// Messages to this account
        /// </summary>
        IEnumerable<IPlayerMessage> Notifications { get; set; }

        /// <summary>
        /// Attempt to restore the config from file
        /// </summary>
        /// <returns>False = no file, True = file</returns>
        bool RestoreConfig(IAccount account);

        /// <summary>
        /// Does this person want this notification
        /// </summary>
        /// <param name="playerName">The player's name who's triggering the notification</param>
        /// <param name="isGossipSystem">Is this the gossip system</param>
        /// <param name="type">what type of notification is this</param>
        /// <returns>Whether or not they want it</returns>
        bool WantsNotification(string playerName, bool isGossipSystem, AcquaintenceNotifications type);

        /// <summary>
        /// Does this person want this notification
        /// </summary>
        /// <param name="playerName">The player's name who's triggering the notification</param>
        /// <param name="isGossipSystem">Is this the gossip system</param>
        /// <param name="type">what type of notification is this</param>
        /// <returns>Whether or not they want it</returns>
        bool IsBlocking(string playerName, bool isGossipSystem);
    }
}