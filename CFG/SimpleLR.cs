using System;
using System.Collections.Generic;
using System.Text;

namespace CFG
{
    class SimpleLR
    {
        // item - of the type A -> a.Xb, where the lhs is a char, and the handle is broken into two parts: 1, an int which represents the position of the ".", and 2, the original string handle taken from the grammar
        // KeyValuePair<char, KeyValuePair<int, string>> - here, char is the lhs of production, int represents the position of the "." and string is the rhs of the production without the "."

        // a set of items
        private HashSet<KeyValuePair<char, KeyValuePair<int, string>>> item = new HashSet<KeyValuePair<char, KeyValuePair<int, string>>>();

        // a canonical collection of sets of items.
        private Dictionary<int, HashSet<KeyValuePair<char, KeyValuePair<int, string>>>> canonicalCollection = new Dictionary<int, HashSet<KeyValuePair<char, KeyValuePair<int, string>>>>();

        // the augmented grammar that this class takes as input.
        private ContextFreeGrammar g;

        class Actions
        {
            Dictionary<string, int> symbols = new Dictionary<string, int>();
            string[,] parsingTable;

            public Actions (ContextFreeGrammar g, SimpleLR slr) {
                int counter = 0;
                foreach (char symbol in g.GetTerminals()) this.symbols.Add(symbol.ToString(), counter++);
                this.symbols.Add("$", counter++);
                foreach (char symbol in g.GetNonTerminals()) {
                    // if (symbol == g.GetStartingSymbol()) continue;
                    this.symbols.Add(symbol.ToString(), counter++);
                }

                parsingTable = new String[slr.canonicalCollection.Count, counter];
                foreach (KeyValuePair<int, HashSet<KeyValuePair<char, KeyValuePair<int, string>>>> state in slr.canonicalCollection) {
                    foreach (KeyValuePair<char, KeyValuePair<int, string>> item in state.Value) {
                        if (/*(item.Value.Key != 0) && */(item.Value.Key < item.Value.Value.Length)/* && (item.Value.Value[item.Value.Key] != g.GetStartingSymbol())*/) {
                            if (g.IsNonTerminal(item.Value.Value[item.Value.Key])) {
                                parsingTable[state.Key, symbols[item.Value.Value[item.Value.Key].ToString()]] = slr.Goto(state.Key, item.Value.Value[item.Value.Key]).ToString();
                            } else if (item.Value.Value[item.Value.Key] == '$') {
                                parsingTable[state.Key, symbols[item.Value.Value[item.Value.Key].ToString()]] = ("accept");
                            } else {
                                parsingTable[state.Key, symbols[item.Value.Value[item.Value.Key].ToString()]] = ("s" + slr.Goto(state.Key, item.Value.Value[item.Value.Key]).ToString());
                            }
                        } else if ((item.Value.Key == item.Value.Value.Length)) {
                            if (item.Value.Value[item.Value.Key - 1] == '$') {
                                parsingTable[state.Key, symbols[item.Value.Value[item.Value.Key].ToString()]] = ("accept");
                            } else {
                                // !! deal with epsilon later
                                foreach (char sym in g.GetFollowSet(item.Value.Value[item.Value.Key - 1])) {
                                    parsingTable[state.Key, symbols[sym.ToString()]] = ("r" + g.GetProductionNumber(item.Key, item.Value.Value));
                                }
                            }
                        }
                    }
                }
            }

            public string[,] GetParsingTable () {
                return this.parsingTable;
            }
            public string GetParsingTableColumnTag (int colNum) {
                foreach (KeyValuePair<string, int> entry in symbols) {
                    if (entry.Value == colNum) return entry.Key;
                }
                return "";
            }
        }
        private Actions actions;

        // constructor calculates the canonical collection, and then generates a parsing table.
        public SimpleLR (ContextFreeGrammar g) {
            this.g = g;
            HashSet<KeyValuePair<char, KeyValuePair<int, string>>> temp = new HashSet<KeyValuePair<char, KeyValuePair<int, string>>>();
            temp.Add(new KeyValuePair<char, KeyValuePair<int, string>>('Θ', new KeyValuePair<int, string>(0, g.GetStartingSymbol().ToString() + "$")));
            HashSet<KeyValuePair<char, KeyValuePair<int, string>>> closure = Closure(temp);

            int stateNum = 0;
            this.canonicalCollection.Add(stateNum++, closure);
            Dictionary<int, HashSet<KeyValuePair<char, KeyValuePair<int, string>>>> tempDict;
            do {
                tempDict = new Dictionary<int, HashSet<KeyValuePair<char, KeyValuePair<int, string>>>>(this.canonicalCollection);
                foreach (KeyValuePair<int, HashSet<KeyValuePair<char, KeyValuePair<int, string>>>> state in tempDict) {
                    foreach (char symbol in g.GetSymbols()) {
                        HashSet<KeyValuePair<char, KeyValuePair<int, string>>> itemSet = this.Goto(state.Value, symbol);
                        if ((itemSet.Count != 0) && (!this.CheckRepeat(canonicalCollection, itemSet))) {
                            this.canonicalCollection.Add(stateNum++, itemSet);
                        }
                    }
                }
            } while (!CompareDicts(tempDict, canonicalCollection));

            actions = new Actions(g, this);
            //actions.AddSymbols(g.GetSymbols());
            //actions.MakeTable(canonicalCollection.Count());
        }

        // special method for checking if an itemSet exists in the canonicalCollection
        private bool CheckRepeat (Dictionary<int, HashSet<KeyValuePair<char, KeyValuePair<int, string>>>> dict, HashSet<KeyValuePair<char, KeyValuePair<int, string>>> itemSet) {
            foreach (HashSet<KeyValuePair<char, KeyValuePair<int, string>>> entry in dict.Values) {
                if (entry.SetEquals(itemSet)) return true;
            }
            return false;
        }

        // !! badly needs optimization.
        // special method for comparing two dictionaries. used to compare canonical collection with its temporary clone.
        private bool CompareDicts (Dictionary<int, HashSet<KeyValuePair<char, KeyValuePair<int, string>>>> one, Dictionary<int, HashSet<KeyValuePair<char, KeyValuePair<int, string>>>> two) {
            if (one.Count != two.Count) return false;
            foreach (KeyValuePair<int, HashSet<KeyValuePair<char, KeyValuePair<int, string>>>> entry in one) if (!two.ContainsKey(entry.Key) || !two[entry.Key].IsSubsetOf(entry.Value) || !entry.Value.IsSubsetOf(two[entry.Key])) return false;
            return true;
        }

        // function takes a set of items as input, and return its closure as output
        // closure is defined as such: if A -> a.Xb exists in the input set, and X -> x exists in the grammar, 
        //     then X -> .x exists in the closure of the input set, i.e, the output item set
        private HashSet<KeyValuePair<char, KeyValuePair<int, string>>> Closure (HashSet<KeyValuePair<char, KeyValuePair<int, string>>> items) {
            HashSet<KeyValuePair<char, KeyValuePair<int, string>>> closure = new HashSet<KeyValuePair<char, KeyValuePair<int, string>>>();
            HashSet<KeyValuePair<char, KeyValuePair<int, string>>> temp;
            foreach (KeyValuePair<char, KeyValuePair<int, string>> item in items) {
                closure.Add(item);
            }
            do {
                temp = new HashSet<KeyValuePair<char, KeyValuePair<int, string>>>(closure);
                foreach (KeyValuePair<char, KeyValuePair<int, string>> item in temp) {
                    if (item.Value.Key == item.Value.Value.Length) continue;
                    char lhs = item.Value.Value[item.Value.Key];
                    if (!g.IsNonTerminal(lhs)) continue;
                    HashSet<string> rhs = g.GetRuleSet(lhs);
                    foreach (string handle in rhs) {
                        closure.Add(new KeyValuePair<char, KeyValuePair<int, string>>(lhs, new KeyValuePair<int, string>(0, handle)));
                    }
                }
            } while (!closure.SetEquals(temp));
            //foreach (KeyValuePair<char, KeyValuePair<int, string>> entry in new HashSet<KeyValuePair<char, KeyValuePair<int, string>>>(closure)) {
            //    if (entry.Key == 'Θ') closure.Remove(entry);
            //}
            return closure;
        }

        // function finds the closure of all items A -> aX.b such that A -> a.Xb exists in items
        private HashSet<KeyValuePair<char, KeyValuePair<int, string>>> Goto (HashSet<KeyValuePair<char, KeyValuePair<int, string>>> items, char symbol) {
            HashSet<KeyValuePair<char, KeyValuePair<int, string>>> itemsNew = new HashSet<KeyValuePair<char, KeyValuePair<int, string>>>();
            foreach (KeyValuePair<char, KeyValuePair<int, string>> item in items) {
                if ((item.Value.Key != item.Value.Value.Length) && (item.Value.Value[item.Value.Key] == symbol)) {
                    itemsNew.Add(new KeyValuePair<char, KeyValuePair<int, string>>(item.Key, new KeyValuePair<int, string>(item.Value.Key + 1, item.Value.Value)));
                }
            }
            return Closure(itemsNew);
        }

        // !! badly needs optimization.
        // function should only be used after canonicalCollection has been fully constructed.
        // function takes an index i and a symbol X as input. It then finds an index j 
        //     such that if A -> a.Xb is in the itemSet at index i, then A -> aX.b is in the itemSet at index j
        //     returns -1 if no such index exists.
        private int Goto (int state, char symbol) {
            HashSet<KeyValuePair<char, KeyValuePair<int, string>>> setx = canonicalCollection[state];
            foreach (KeyValuePair<char, KeyValuePair<int, string>> element in setx) {
                if (element.Value.Key != element.Value.Value.Length && element.Value.Value[element.Value.Key] == symbol) {
                    foreach (KeyValuePair<int, HashSet<KeyValuePair<char, KeyValuePair<int, string>>>> set in canonicalCollection) {
                        foreach (KeyValuePair<char, KeyValuePair<int, string>> item in set.Value) {
                            if (element.Value.Value.Equals(item.Value.Value, StringComparison.Ordinal) && ((item.Value.Key - element.Value.Key) == 1)) return set.Key;
                        }
                    }
                }
            }
            return -1;
        }

        // basic getter
        public Dictionary<int, HashSet<KeyValuePair<char, KeyValuePair<int, string>>>> GetCanonicalCollection () {
            return this.canonicalCollection;
        }

        // basic getter
        public HashSet<KeyValuePair<char, KeyValuePair<int, string>>> GetItemSet (int stateNum) {
            return this.canonicalCollection[stateNum];
        }

        // basic getter
        public string[,] GetParsingTable () {
            return this.actions.GetParsingTable();
        }

        // basic getter
        public string GetParsingTableColumnTag (int colNum) {
            return this.actions.GetParsingTableColumnTag(colNum);
        }
    }
}
