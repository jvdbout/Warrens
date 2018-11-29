﻿using NetMud.DataStructure.Linguistic;
using NetMud.DataStructure.SupportingClasses;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NetMud.Data.Lexical
{
    [Serializable]
    public class Occurrence : IOccurrence
    {
        /// <summary>
        /// The thing happening
        /// </summary>
        public ILexica Event { get; set; }

        /// <summary>
        /// The perceptive strength (higher = easier to see and greater distance noticed)
        /// </summary>
        public int Strength { get; set; }

        /// <summary>
        /// The type of sense used to detect this
        /// </summary>
        public MessagingType SensoryType { get; set; }

        public Occurrence()
        {

        }

        public Occurrence(ILexica happening, int strength, MessagingType sensoryType)
        {
            Event = happening;
            Strength = strength;
            SensoryType = sensoryType;
        }

        /// <summary>
        /// Use this to create a "blank" occurrence
        /// </summary>
        public Occurrence(MessagingType sensoryType)
        {
            SensoryType = sensoryType;
            Strength = -1;
            Event = new Lexica(LexicalType.Noun, GrammaticalType.Descriptive, string.Empty);
        }

        /// <summary>
        /// Try to add a modifier to a lexica
        /// </summary>
        /// <param name="modifier">the lexica that is the modifier</param>
        /// <returns>Whether or not it succeeded</returns>
        public ILexica TryModify(ILexica modifier, bool passthru = false)
        {
            return Event.TryModify(modifier, passthru);
        }

        /// <summary>
        /// Try to add a modifier to a lexica
        /// </summary>
        /// <param name="modifier">the lexica that is the modifier</param>
        /// <returns>Whether or not it succeeded</returns>
        public void TryModify(ILexica[] modifier)
        {
            Event.TryModify(modifier);
        }

        /// <summary>
        /// Try to add a modifier to a lexica
        /// </summary>
        /// <param name="modifier">the lexica that is the modifier</param>
        /// <returns>Whether or not it succeeded</returns>
        public void TryModify(IEnumerable<ILexica> modifier)
        {
            Event.TryModify(modifier);
        }

        /// <summary>
        /// Try to add a modifier to a lexica
        /// </summary>
        /// <param name="modifier">the lexica that is the modifier</param>
        /// <returns>Whether or not it succeeded</returns>
        public ILexica TryModify(IOccurrence modifier, bool passthru = false)
        {
            return TryModify(modifier.Event, passthru);
        }

        /// <summary>
        /// Try to add a modifier to a lexica
        /// </summary>
        /// <param name="modifier">the lexica that is the modifier</param>
        /// <returns>Whether or not it succeeded</returns>
        public void TryModify(IOccurrence[] modifier)
        {
            TryModify(modifier.Select(occ => occ.Event));
        }

        /// <summary>
        /// Try to add a modifier to a lexica
        /// </summary>
        /// <param name="modifier">the lexica that is the modifier</param>
        /// <returns>Whether or not it succeeded</returns>
        public void TryModify(IEnumerable<IOccurrence> modifier)
        {
            TryModify(modifier.Select(occ => occ.Event));
        }

        /// <summary>
        /// Try to add a modifier to a lexica
        /// </summary>
        /// <param name="modifier">the lexica that is the modifier</param>
        /// <returns>Whether or not it succeeded</returns>
        public ILexica TryModify(LexicalType type, GrammaticalType role, string phrase, bool passthru = false)
        {
            return Event.TryModify(type, role, phrase, passthru);
        }

        /// <summary>
        /// Try to add a modifier to a lexica
        /// </summary>
        /// <param name="modifier">the lexica that is the modifier</param>
        /// <returns>Whether or not it succeeded</returns>
        public ILexica TryModify(Tuple<LexicalType, GrammaticalType, string> modifier, bool passthru = false)
        {
            return TryModify(modifier.Item1, modifier.Item2, modifier.Item3, passthru);
        }

        /// <summary>
        /// Try to add a modifier to a lexica
        /// </summary>
        /// <param name="modifier">the lexica that is the modifier</param>
        /// <returns>Whether or not it succeeded</returns>
        public void TryModify(Tuple<LexicalType, GrammaticalType, string>[] modifier)
        {
            Event.TryModify(modifier);
        }

        /// <summary>
        /// Create a narrative description from this
        /// </summary>
        /// <param name="normalization">How much sentence splitting should be done</param>
        /// <param name="verbosity">A measure of how much flourish should be added as well as how far words get synonym-upgraded by "finesse". (0 to 100)</param>
        /// <param name="chronology">The time tensing of the sentence structure</param>
        /// <param name="perspective">The personage of the sentence structure</param>
        /// <param name="omitName">Should we omit the proper name of the initial subject entirely (and only resort to pronouns)</param>
        /// <returns>A long description</returns>
        public string Describe(NarrativeNormalization normalization, int verbosity, LexicalTense chronology = LexicalTense.Present,
            NarrativePerspective perspective = NarrativePerspective.SecondPerson, bool omitName = true)
        {
            return Event.Describe(normalization, verbosity, chronology, perspective, omitName);
        }

        /// <summary>
        /// Render this lexica to a sentence fragment (or whole sentence if it's a Subject role)
        /// </summary>
        /// <returns>a sentence fragment</returns>
        public override string ToString()
        {
            return Event.ToString();
        }
    }
}