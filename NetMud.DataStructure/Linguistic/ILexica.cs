﻿using NetMud.DataStructure.Architectural.EntityBase;
using System;
using System.Collections.Generic;

namespace NetMud.DataStructure.Linguistic
{
    /// <summary>
    /// A gramatical element
    /// </summary>
    public interface ILexica : IComparable<ILexica>, IEquatable<ILexica>
    {
        /// <summary>
        /// The type of word this is to the sentence
        /// </summary>
        GrammaticalType Role { get; set; }

        /// <summary>
        /// The type of word this is in general
        /// </summary>
        LexicalType Type { get; set; }

        /// <summary>
        /// The actual word/phrase
        /// </summary>
        string Phrase { get; set; }

        /// <summary>
        /// Modifiers for this lexica. (Modifier, Conjunction)
        /// </summary>
        HashSet<ILexica> Modifiers { get; set; }

        /// <summary>
        /// Try to add a modifier to a lexica
        /// </summary>
        /// <param name="modifier">the lexica that is the modifier</param>
        /// <returns>Whether or not it succeeded</returns>
        ILexica TryModify(ILexica modifier, bool passthru = false);

        /// <summary>
        /// Try to add a modifier to a lexica
        /// </summary>
        /// <param name="modifier">the lexica that is the modifier</param>
        /// <returns>Whether or not it succeeded</returns>
        void TryModify(ILexica[] modifiers);

        /// <summary>
        /// Try to add a modifier to a lexica
        /// </summary>
        /// <param name="modifier">the lexica that is the modifier</param>
        /// <returns>Whether or not it succeeded</returns>
        void TryModify(IEnumerable<ILexica> modifiers);

        /// <summary>
        /// Try to add a modifier to a lexica
        /// </summary>
        /// <param name="modifier">the lexica that is the modifier</param>
        /// <returns>Whether or not it succeeded</returns>
        void TryModify(HashSet<ILexica> modifiers);

        /// <summary>
        /// Try to add a modifier to a lexica
        /// </summary>
        /// <param name="modifier">the lexica that is the modifier</param>
        /// <returns>Whether or not it succeeded</returns>
        ILexica TryModify(LexicalType type, GrammaticalType role, string phrase, bool passthru = false);

        /// <summary>
        /// Try to add a modifier to a lexica
        /// </summary>
        /// <param name="modifier">the lexica that is the modifier</param>
        /// <returns>Whether or not it succeeded</returns>
        ILexica TryModify(Tuple<LexicalType, GrammaticalType, string> modifier, bool passthru = false);

        /// <summary>
        /// Try to add a modifier to a lexica
        /// </summary>
        /// <param name="modifier">the lexica that is the modifier</param>
        /// <returns>Whether or not it succeeded</returns>
        void TryModify(Tuple<LexicalType, GrammaticalType, string>[] modifier);

        /// <summary>
        /// Create a narrative description from this
        /// </summary>
        /// <param name="language">What language this should output in</param>
        /// <param name="severity">Severity delta modifier to find synonyms with</param>
        /// <param name="eloquence">eloquence delta modifier to find synonyms with</param>
        /// <param name="quality">quality delta modifier to find synonyms with</param>
        /// <param name="normalization">How much sentence splitting should be done</param>
        /// <param name="verbosity">A measure of how much flourish should be added as well as how far words get synonym-upgraded by "finesse". (0 to 100)</param>
        /// <param name="chronology">The time tensing of the sentence structure</param>
        /// <param name="perspective">The personage of the sentence structure</param>
        /// <param name="omitName">Should we omit the proper name of the initial subject entirely (and only resort to pronouns)</param>
        /// <returns>A long description</returns>
        string Unpack(LexicalContext context, NarrativeNormalization normalization, bool omitName = true);

        /// <summary>
        /// Describe the lexica
        /// </summary>
        /// <returns></returns>
        string Describe(LexicalContext context);

        /// <summary>
        /// Alter the lex entirely including all of its sublex
        /// </summary>
        /// <param name="language">the new language</param>
        /// <param name="severity">the severity delta</param>
        /// <param name="eloquence">the eloquence delta</param>
        /// <param name="quality">the quality delta</param>
        /// <returns>the new lex</returns>
        ILexica Mutate(LexicalContext context);

        /// <summary>
        /// Build out the context object
        /// </summary>
        /// <param name="entity">the subject</param>
        LexicalContext BuildContext(IEntity entity);

        /// <summary>
        /// Get the dictata from this lexica
        /// </summary>
        /// <returns>A dictata</returns>
        IDictata GetDictata();

        /// <summary>
        /// Generates a new dictata
        /// </summary>
        /// <returns>success</returns>
        bool GenerateDictata();
    }
}
