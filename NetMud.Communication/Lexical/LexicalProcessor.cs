﻿using NetMud.DataAccess;
using NetMud.DataAccess.Cache;
using NetMud.DataStructure.Architectural;
using NetMud.DataStructure.Linguistic;
using NetMud.DataStructure.System;
using NetMud.Lexica.DeepLex;
using NetMud.Utility;
using Syn.WordNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Caching;
using System.Text.RegularExpressions;
using System.Web;

namespace NetMud.Communication.Lexical
{
    /// <summary>
    /// Processes Lexica and outputs formatted prose
    /// </summary>
    public static class LexicalProcessor
    {
        private static readonly ObjectCache globalCache = MemoryCache.Default;
        private static readonly CacheItemPolicy globalPolicy = new CacheItemPolicy();
        private static readonly string wordNetTokenCacheKey = "WordNetHarness";
        private static readonly string mirriamWebsterTokenCacheKey = "MirriamHarness";

        public static Syn.WordNet.WordNetEngine  WordNetHarness
        {
            get
            {
                return (Syn.WordNet.WordNetEngine)globalCache[wordNetTokenCacheKey];
            }
            set
            {
                globalCache.AddOrGetExisting(wordNetTokenCacheKey, value, globalPolicy);
            }
        }

        public static MirriamWebsterHarness MirriamWebsterAPI
        {
            get
            {
                return (MirriamWebsterHarness)globalCache[mirriamWebsterTokenCacheKey];
            }
            set
            {
                globalCache.AddOrGetExisting(mirriamWebsterTokenCacheKey, value, globalPolicy);
            }
        }

        /// <summary>
        /// Map the synonyms of this
        /// </summary>
        /// <param name="dictata">the word in question</param>
        /// <returns>a trigger boolean to end a loop</returns>
        public static bool GetSynSet(IDictata dictata)
        {
            try
            {
                if (dictata.WordType != LexicalType.None)
                {
                    var wordList = new List<string>();
                    CreateOrModifyLexeme(dictata.Language, dictata.Name, dictata.WordType, ref wordList);
                }
            }
            catch (Exception ex)
            {
                LoggingUtility.LogError(ex);
                //don't barf on this
            }

            return false;
        }

        /// <summary>
        /// Create or modify a lexeme with no word form basis, gets tricky with best fit scenarios
        /// </summary>
        /// <param name="word">just the text of the word</param>
        /// <returns>A lexeme</returns>
        public static ILexeme CreateOrModifyLexeme(ILanguage language, string word, LexicalType wordType, ref List<string> processedWords)
        {
            word = word.ToLower();

            Regex rgx = new Regex("[^a-z -]");
            word = rgx.Replace(word, "");

            if (string.IsNullOrWhiteSpace(word) || word.All(ch => ch == '-'))
            {
                return null;
            }

            ILexeme newLex = ConfigDataCache.Get<ILexeme>(string.Format("{0}_{1}_{2}", ConfigDataType.Dictionary, language.Name, word));

            if (newLex == null)
            {
                newLex = language.CreateOrModifyLexeme(word, wordType, new string[0]);
            }

            if (newLex.IsSynMapped || processedWords.Any(wrd => wrd.Equals(word)))
            {
                if (!processedWords.Any(wrd => wrd.Equals(word)))
                {
                    processedWords.Add(word);
                }

                return newLex;
            }

            LexicalType[] invalidTypes = new LexicalType[] { LexicalType.Article, LexicalType.Conjunction, LexicalType.ProperNoun, LexicalType.Pronoun, LexicalType.None };

            processedWords.Add(word);

            //This is wordnet processing, wordnet doesnt have any of the above and will return weird results if we let it
            if (!invalidTypes.Contains(wordType))
            {
                var synSets = WordNetHarness.GetSynSets(word, new PartOfSpeech[] { PartOfSpeech.Adjective, PartOfSpeech.Adverb, PartOfSpeech.Noun, PartOfSpeech.Verb });

                //We in theory have every single word form for this word now
                if (synSets != null)
                {
                    foreach (SynSet synSet in synSets)
                    {
                        if (synSet.PartOfSpeech == PartOfSpeech.None)
                            continue;

                        var newDict = newLex.GetForm(MapLexicalTypes(synSet.PartOfSpeech), -1);

                        if(newDict == null)
                        {
                            newLex = language.CreateOrModifyLexeme(word, MapLexicalTypes(synSet.PartOfSpeech), new string[0]);
                            newDict = newLex.GetForm(MapLexicalTypes(synSet.PartOfSpeech), -1);
                        }

                        //grab semantics somehow
                        List<string> semantics = new List<string>();

                        if (!string.IsNullOrWhiteSpace(synSet.Gloss))
                        {
                            var indexSplit = synSet.Gloss.IndexOf(';');
                            string definition = synSet.Gloss.Substring(0, indexSplit < 0 ? synSet.Gloss.Length - 1 : indexSplit).Trim();
                            string[] defWords = definition.Split(' ');

                            foreach (string defWord in defWords)
                            {
                                var currentWord = defWord.ToLower();
                                currentWord = rgx.Replace(currentWord, "");

                                //if (processedWords.Contains(currentWord))
                                //    continue;

                                if (currentWord.Equals(word) || string.IsNullOrWhiteSpace(word) || word.All(ch => ch == '-') || word.IsNumeric())
                                {
                                    continue;
                                }

                                //var defLex = language.CreateOrModifyLexeme(currentWord, MapLexicalTypes(synSet.PartOfSpeech), new string[0]);

                                semantics.Add(currentWord);
                            }
                        }

                        ///wsns indicates hypo/hypernymity so
                        foreach (string synWord in synSet.Words)
                        {
                            var newWord = synWord.ToLower();
                            newWord = rgx.Replace(newWord, "");

                            if (processedWords.Contains(newWord))
                                continue;

                            ///wsns indicates hypo/hypernymity so
                            int mySeverity = 1;
                            int myElegance = Math.Max(0, synWord.SyllableCount() * 3);
                            int myQuality = 2;

                            //it's a phrase
                            if (synWord.Contains("_"))
                            {
                                string[] words = synWord.Split('_');

                                List<ILexeme> phraseList = new List<ILexeme>();
                                foreach (string phraseWord in words)
                                {
                                    //make the phrase? maybe later
                                    ILexeme phraseLex = ConfigDataCache.Get<ILexeme>(string.Format("{0}_{1}_{2}", ConfigDataType.Dictionary, language.Name, phraseWord));
                                    if(phraseLex != null)
                                    {
                                        phraseList.Add(phraseLex);
                                    }
                                }


                                if(phraseList.Count == words.Length)
                                {
                                    var phrase = language.CreateOrModifyPhrase(phraseList.Select(phr => phr.GetForm(MapLexicalTypes(synSet.PartOfSpeech), -1)),
                                                                                MapLexicalTypes(synSet.PartOfSpeech),
                                                                                semantics.ToArray(),
                                                                                mySeverity, myElegance, myQuality, 
                                                                                newDict.Feminine, newDict.Perspective, newDict.Positional, newDict.Tense);

                                    newDict.MakeRelatedPhrase(phrase, true);
                                }
                            }
                            else
                            {
                                processedWords.Add(newWord);

                                if (string.IsNullOrWhiteSpace(newWord) || newWord.All(ch => ch == '-') || newWord.IsNumeric())
                                {
                                    continue;
                                }

                                var synLex = language.CreateOrModifyLexeme(newWord, MapLexicalTypes(synSet.PartOfSpeech), semantics.ToArray());

                                var synDict = synLex.GetForm(MapLexicalTypes(synSet.PartOfSpeech), semantics.ToArray(), false);
                                synDict.Elegance = Math.Max(0, newDict.Name.SyllableCount() * 3);
                                synDict.Quality = synSet.Words.Count();
                                synDict.Severity = 2;

                                synLex.PersistToCache();
                                synLex.SystemSave();

                                if (!newWord.Equals(word))
                                {
                                    newDict.MakeRelatedWord(language, newWord, true, synDict);
                                }
                            }
                        }
                    }
                }
            }

            newLex.IsSynMapped = true;
            newLex.SystemSave();
            newLex.PersistToCache();

            return newLex;
        }

        /// <summary>
        /// Verify the dictionary has this word already
        /// </summary>
        /// <param name="lexica">lexica to check</param>
        public static void VerifyLexeme(ILexica lexica)
        {
            if (lexica == null || string.IsNullOrWhiteSpace(lexica.Phrase) || lexica.Phrase.IsNumeric())
            {
                //we dont want numbers getting in the dict, thats bananas
                return;
            }

            VerifyLexeme(lexica.GetDictata().GetLexeme());
        }

        /// <summary>
        /// Verify the dictionary has this word already
        /// </summary>
        /// <param name="lexeme">dictata to check</param>
        public static ILexeme VerifyLexeme(ILexeme lexeme)
        {
            if (lexeme == null || string.IsNullOrWhiteSpace(lexeme.Name) || lexeme.Name.IsNumeric())
            {
                return null;
            }

            var deepLex = false;
            //Set the language to default if it is absent and save it, if it has a language it already exists
            if (lexeme.Language == null)
            {
                IGlobalConfig globalConfig = ConfigDataCache.Get<IGlobalConfig>(new ConfigDataCacheKey(typeof(IGlobalConfig), "LiveSettings", ConfigDataType.GameWorld));

                if (globalConfig.BaseLanguage != null)
                {
                    lexeme.Language = globalConfig.BaseLanguage;
                }

                deepLex = globalConfig?.DeepLexActive ?? false;
            }

            ILexeme maybeLexeme = ConfigDataCache.Get<ILexeme>(string.Format("{0}_{1}_{2}", ConfigDataType.Dictionary, lexeme.Language, lexeme.Name));

            if (maybeLexeme != null)
            {
                lexeme = maybeLexeme;
            }

            lexeme.IsSynMapped = false;

            lexeme.PersistToCache();
            lexeme.SystemSave();
            lexeme.MapSynNet();

            return lexeme;
        }

        public static string GetPunctuationMark(SentenceType type, bool upsideDown = false)
        {
            string punctuation = string.Empty;
            switch (type)
            {
                case SentenceType.Exclamation:
                    punctuation = upsideDown ? "!" : "!";
                    break;
                case SentenceType.ExclamitoryQuestion:
                    punctuation = upsideDown ? "?!" : "?!";
                    break;
                case SentenceType.Partial:
                    punctuation = ";";
                    break;
                case SentenceType.Question:
                    punctuation = upsideDown ? "?" : "?";
                    break;
                case SentenceType.Statement:
                case SentenceType.None:
                    punctuation = ".";
                    break;
            }

            return punctuation;
        }

        public static void LoadWordnet()
        {
            string wordNetPath = HttpContext.Current.Server.MapPath("/FileStore/wordnet/");

            if (!Directory.Exists(wordNetPath))
            {
                LoggingUtility.LogError(new FileNotFoundException("WordNet data not found."));
                return;
            }

            var engine = new WordNetEngine();
            engine.LoadFromDirectory(wordNetPath);
            WordNetHarness = engine;
        }

        public static void LoadMirriamHarness(string dictKey, string thesaurusKey)
        {
            MirriamWebsterAPI = new MirriamWebsterHarness(dictKey, thesaurusKey);
        }

        public static LexicalType MapLexicalTypes(PartOfSpeech pos)
        {
            switch (pos)
            {
                case PartOfSpeech.Adjective:
                    return LexicalType.Adjective;
                case PartOfSpeech.Adverb:
                    return LexicalType.Adverb;
                case PartOfSpeech.Noun:
                    return LexicalType.Noun;
                case PartOfSpeech.Verb:
                    return LexicalType.Verb;
            }

            return LexicalType.None;
        }

        public static PartOfSpeech MapLexicalTypes(LexicalType pos)
        {
            switch (pos)
            {
                case LexicalType.Adjective:
                    return Syn.WordNet.PartOfSpeech.Adjective;
                case LexicalType.Adverb:
                    return Syn.WordNet.PartOfSpeech.Adverb;
                case LexicalType.Noun:
                    return Syn.WordNet.PartOfSpeech.Noun;
                case LexicalType.Verb:
                    return Syn.WordNet.PartOfSpeech.Verb;
            }

            return Syn.WordNet.PartOfSpeech.None;
        }
    }
}
