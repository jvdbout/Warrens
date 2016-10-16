﻿using NetMud.DataAccess.Cache;
using NetMud.DataStructure.Base.EntityBackingData;
using NetMud.DataStructure.Base.Place;
using NetMud.DataStructure.SupportingClasses;
using System;
using System.Linq;
using System.Text;

namespace NetMud.Cartography
{
    /// <summary>
    /// General set of methods to render rooms, zones and worlds into ascii maps
    /// </summary>
    public static class Rendering
    {
        /// <summary>
        /// Render an ascii map of live rooms around a specific radius (always includes pathways, never includes editing links
        /// </summary>
        /// <param name="room">the room to render the radius around</param>
        /// <param name="radius">the radius around the room to render</param>
        /// <returns>a single string that is an ascii map</returns>
        public static string RenderRadiusMap(IRoom room, int radius)
        {
            var asciiMapString = new StringBuilder();

            // RenderRoomDataForEditWithRadius(asciiMapString, room, radius);

            return asciiMapString.ToString();
        }

        /// <summary>
        /// Render an ascii map of stored data rooms around a specific radius
        /// </summary>
        /// <param name="room">the room to render the radius around</param>
        /// <param name="radius">the radius around the room to render</param>
        /// <param name="forAdmin">include edit links for paths and rooms?</param>
        /// <param name="withPathways">include paths at all?</param>
        /// <returns>a single string that is an ascii map</returns>
        public static string RenderRadiusMap(IRoomData room, int radius, bool forAdmin = true, bool withPathways = true)
        {
            var asciiMapString = new StringBuilder();

            RenderRoomWithRadius(asciiMapString, room, radius, forAdmin, withPathways);

            return asciiMapString.ToString();
        }

        private static string RenderRoomWithRadius(StringBuilder sb, IRoomData centerRoom, int radius, bool forAdmin, bool withPathways)
        {
            if (centerRoom == null || radius < 0)
                return "Invalid inputs.";

            //triple radius (one for pathway, one for return pathway, one for rooms) in each direction plus the center room
            var diameter = withPathways ? radius * 6 : radius * 2;

            //Useful to have, we dont want math all over the code just to find the center every time
            var center = withPathways ? radius * 3 : radius;

            //+1 for center room
            diameter++;
            center++;

            var asciiMap = new string[diameter, diameter];

            //The origin room
            asciiMap = RenderFullRoomToAscii(asciiMap, centerRoom, diameter, center, center, true);

            int x, y;
            for (y = diameter - 1; y >= 0; y--)
            {
                var rowString = String.Empty;
                for (x = 0; x < diameter; x++)
                    rowString += asciiMap[x, y];

                sb.AppendLine(rowString);
            }

            //We add an entire extra 2 lines to do non-compass direction rooms
            var extraString = String.Empty;
            foreach (var path in centerRoom.GetPathways().Where(path => path.DirectionType == MovementDirectionType.None
                                                        && !string.IsNullOrWhiteSpace(path.ToLocationID)
                                                        && path.ToLocationType.Equals("Room")))
                extraString += "&nbsp;" + RenderPathwayToAscii(path, centerRoom.ID, MovementDirectionType.None);

            //One last for allowance of adding non-directional ones
            extraString += "&nbsp;" + RenderPathwayToAscii(null, centerRoom.ID, MovementDirectionType.None);

            if (extraString.Length > 0)
            {
                sb.AppendLine("&nbsp;");
                sb.AppendLine(extraString);
            }

            return sb.ToString();
        }

        //It's just easier to pass the ints we already calculated along instead of doing the math every single time, this cascades each direction fully because it calls itself for existant rooms
        private static string[,] RenderFullRoomToAscii(string[,] asciiMap, IRoomData origin, int diameter, int centerX, int centerY, bool center = false)
        {
            //Render the room itself
            asciiMap[centerX - 1, centerY - 1] = RenderRoomToAscii(origin, center);
            asciiMap = RenderDirection(asciiMap, MovementDirectionType.North, origin, diameter, centerX, centerY);
            asciiMap = RenderDirection(asciiMap, MovementDirectionType.NorthEast, origin, diameter, centerX, centerY);
            asciiMap = RenderDirection(asciiMap, MovementDirectionType.NorthWest, origin, diameter, centerX, centerY);
            asciiMap = RenderDirection(asciiMap, MovementDirectionType.East, origin, diameter, centerX, centerY);
            asciiMap = RenderDirection(asciiMap, MovementDirectionType.West, origin, diameter, centerX, centerY);
            asciiMap = RenderDirection(asciiMap, MovementDirectionType.South, origin, diameter, centerX, centerY);
            asciiMap = RenderDirection(asciiMap, MovementDirectionType.SouthEast, origin, diameter, centerX, centerY);
            asciiMap = RenderDirection(asciiMap, MovementDirectionType.SouthWest, origin, diameter, centerX, centerY);

            return asciiMap;
        }

        //We have to render our pathway out, an empty space for the potential pathway back and the destination room
        private static string[,] RenderDirection(string[,] asciiMap, MovementDirectionType transversalDirection, IRoomData origin, int diameter, int centerX, int centerY)
        {
            var pathways = origin.GetPathways();
            var directionalSteps = Utilities.GetDirectionStep(transversalDirection);

            var xStepped = centerX + directionalSteps.Item1;
            var yStepped = centerY + directionalSteps.Item2;

            //If we're not over diameter budget and there is nothing there already (we might have already rendered the path and room) then render it
            //When the next room tries to render backwards it'll run into the existant path it came from and stop the chain here
            if (xStepped <= diameter && yStepped <= diameter && xStepped > 0 && yStepped > 0
                && String.IsNullOrWhiteSpace(asciiMap[xStepped - 1, yStepped - 1]))
            {
                var thisPath = pathways.FirstOrDefault(path => path.DirectionType == transversalDirection);
                asciiMap[xStepped - 1, yStepped - 1] = RenderPathwayToAscii(thisPath, origin.ID, transversalDirection);

                //We triple step here because the first step was the pathway but the second step is blank for the return pathway. the third step is the actual room
                var tripleXStep = xStepped + directionalSteps.Item1 * 2;
                var tripleYStep = yStepped + directionalSteps.Item2 * 2;

                if (thisPath != null && thisPath.ToLocationType.Equals("Room", StringComparison.InvariantCultureIgnoreCase)
                    && tripleXStep <= diameter && tripleYStep <= diameter && tripleXStep > 0 && tripleYStep > 0
                    && String.IsNullOrWhiteSpace(asciiMap[tripleXStep - 1, tripleYStep - 1]))
                {
                    var passdownOrigin = BackingDataCache.Get<IRoomData>(long.Parse(thisPath.ToLocationID));

                    if (passdownOrigin != null)
                        asciiMap = RenderFullRoomToAscii(asciiMap, passdownOrigin, diameter, tripleXStep, tripleYStep);
                }
            }

            return asciiMap;
        }

        private static string RenderPathwayToAscii(IPathwayData path, long originId, MovementDirectionType directionType)
        {
            var returnValue = String.Empty;
            var asciiCharacter = Utilities.TranslateDirectionToAsciiCharacter(directionType);

            if (path != null)
            {
                var destination = BackingDataCache.Get<IRoomData>(long.Parse(path.ToLocationID));

                returnValue = String.Format("<a href='/GameAdmin/Pathway/Edit/{0}' target='_blank' class='editData pathway' title='Edit - {1}' data-id='{0}'>{2}</a>",
                    path.ID, destination.Name, asciiCharacter);
            }
            else
                returnValue = String.Format("<a href='/GameAdmin/Pathway/Add/{0}' class='addData pathway' target='_blank' data-direction='{1}' title='Add - {2} path and room'>+</a>",
                    originId, Utilities.TranslateDirectionToDegrees(directionType), directionType.ToString());

            return returnValue;
        }

        private static string RenderRoomToAscii(IRoomData destination, bool centered)
        {
            var character = centered ? "0" : "*";

            return String.Format("<a href='/GameAdmin/Room/Edit/{0}' class='editData room' target='_blank' title='Edit - {2}'>{1}</a>", destination.ID, character, destination.Name);
        }

    }
}