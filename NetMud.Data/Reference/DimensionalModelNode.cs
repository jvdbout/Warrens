﻿using NetMud.DataStructure.Base.Supporting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMud.Data.Reference
{
    [Serializable]
    public class DimensionalModelNode : IDimensionalModelNode
    {
        /// <summary>
        /// The position of this node on the XAxis
        /// </summary>
        public short XAxis { get; set; }

        /// <summary>
        /// The position of this node on the ZAxis
        /// </summary>
        public short ZAxis { get; set; }

        /// <summary>
        /// The damage type inflicted when this part of the model strikes
        /// </summary>
        public DamageType Style { get; set; }

        /// <summary>
        /// Material composition of the node
        /// </summary>
        public IMaterial Composition { get; set; }
    }
}