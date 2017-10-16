using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CFG
{

    class ContextFreeGrammar
    {
        private Dictionary<char, HashSet<string>> productions = new Dictionary<char, HashSet<string>>();
        private Dictionary<char, HashSet<char>> firsts = new Dictionary<char, HashSet<char>>();
        private Dictionary<char, HashSet<char>> follows = new Dictionary<char, HashSet<char>>();
        private HashSet<char> symbols = new HashSet<char>();
        private HashSet<char> nonTerminals = new HashSet<char>();
        private HashSet<char> terminals = new HashSet<char>();
        private char startingSymbol = ' ';
        private Dictionary<string, HashSet<KeyValuePair<string, int>>> ordering = new Dictionary<string, HashSet<KeyValuePair<string, int>>>();
        private int counter = 1;

        public ContextFreeGrammar () { }

        public ContextFreeGrammar (string filepath) {
            string line;
            FileStream fileStream = new FileStream(filepath, FileMode.Open);

            using (StreamReader reader = new StreamReader(fileStream)) {
                while ((line = reader.ReadLine()) != null) {
                    string[] components = line.Replace(" ", String.Empty).Split(new[] { "->", "|" }, StringSplitOptions.None);

                    // if the current line is supposed to be a comment or is empty, then skip it
                    if (line == String.Empty || (components[0][0] == '/' && components[0][1] == '/')) continue;

                    // add the first character to of the first string into the set of non-terminals
                    //nonTerminals.Add(components[0][0]);

                    // check if we have set the starting symbol. if not, set it. Otherwise leave it untouched.
                    startingSymbol = (startingSymbol == ' ') ? components[0][0] : startingSymbol;

                    // add all of the symbols in this line to the set of symbols.
                    foreach (string str in components) foreach (char c in str) symbols.Add(c);

                    //// if we have seen the LHS of this production before, get set of handles associated with it currently
                    //// otherwise make a new set of handles.
                    //if (productions.ContainsKey(components[0][0])) {
                    //    set = this.productions[components[0][0]];
                    //    productions.Remove(components[0][0]);
                    //} else set = new HashSet<string>();

                    //// add all handles in the current  line into it, and then add it to the dictionary of all productions
                    //for (int i = 1; i < components.Length; i++) set.Add(components[i]);
                    //productions.Add(components[0][0], set);
                    this.AddProduction(components[0][0], components.Skip(1).ToArray<string>());
                }
            }

            // now put all symbols in the set of terminals, and just for safety, all symbols in the set of non-terminals into 
            // the set of all symbols, and then subtract the set of non-terminals from the set of terminals
            terminals.UnionWith(symbols);
            symbols.UnionWith(nonTerminals);
            terminals.ExceptWith(nonTerminals);

            // find firsts
            Dictionary<char, HashSet<char>> temp;
            do {
                //Console.WriteLine("\nDoing one pass on firsts...");
                temp = new Dictionary<char, HashSet<char>>(firsts);
                foreach (char symbol in nonTerminals) {
                    firsts.Add(symbol, First(symbol));
                    //Console.Write(symbol + " to {");
                    //foreach (char sym in firsts[symbol]) Console.Write(" " + sym + " ");
                    //Console.WriteLine("}");
                }
            } while (!CompareDicts(firsts, temp));
            //Console.WriteLine("\n");

            // find follows
            HashSet<char> tempSet = new HashSet<char>(), carrySet;
            // add '$' to the follow set of the starting symbol.
            tempSet.Add('$');
            follows.Add(startingSymbol, tempSet);
            // as long as the follow keeps changing, do all this
            do {
                // copy the current follow into temp
                temp = new Dictionary<char, HashSet<char>>(follows);
                // iterate over each production
                foreach (KeyValuePair<char, HashSet<string>> rule in productions) {
                    // iterate over each handle in the current production
                    foreach (string handle in rule.Value) {
                        // make a new carry set
                        carrySet = new HashSet<char>();

                        // if the LHS non-terminal in this production has appeared before, get its current follow set, and union it with the carry set.
                        // this is because the last symbol in all handles inherits the follow set of the LHS non-terminal
                        if (follows.ContainsKey(rule.Key)) carrySet.UnionWith(follows[rule.Key]);

                        // for each symbol in this handle, going right to left, do all this:-
                        for (int i = handle.Length - 1; i >= 0; i--) {
                            // if an entry for this symbol already exists in the follow sets, get it
                            // otherwise make a new, empty set.
                            if (this.follows.ContainsKey(handle[i])) {
                                tempSet = this.follows[handle[i]];
                                follows.Remove(handle[i]);
                            } else tempSet = new HashSet<char>();

                            // add the contents of the current carry set into the temporary follow for this symbol.
                            tempSet.UnionWith(carrySet);

                            // if the first set of the current symbol contains 'ε', then add the contents of this first set into carry set
                            // and then remove the symbol 'ε' from the final set.
                            // otherwise overwrite the carry set with the first set for the current symbol
                            if (this.GetFirstSet(handle[i]).Contains('ε')) {
                                carrySet.UnionWith(this.GetFirstSet(handle[i]));
                                carrySet.Remove('ε');
                            } else carrySet = new HashSet<char>(this.GetFirstSet(handle[i]));

                            // add this symbol and its follow set to the dictionary of all follow sets.
                            follows.Add(handle[i], tempSet);
                        }
                    }
                }
            } while (!CompareDicts(follows, temp));

            LeftFactor();
            //Console.WriteLine("\n");
        }

        private bool CompareDicts (Dictionary<char, HashSet<char>> one, Dictionary<char, HashSet<char>> two) {
            if (one.Count() != two.Count()) return false;
            foreach (KeyValuePair<char, HashSet<char>> entry in one) if (!two.ContainsKey(entry.Key) || !two[entry.Key].IsSubsetOf(entry.Value) || !entry.Value.IsSubsetOf(two[entry.Key])) return false;
            return true;
        }

        private HashSet<char> First (char symbol) {
            //Console.Write(" -" + symbol + " ");
            HashSet<char> first, firstTemp;
            // if not a non-terminal character, the first set is a singleton set containing the character itself.
            if (!nonTerminals.Contains(symbol)) {
                //Console.Write("-- " + !nonTerminals.Contains(symbol) + " ");
                first = new HashSet<char>();
                first.Add(symbol);
            } else {
                // if this non terminal has already been dealt with before, get its current first set.
                if (this.firsts.ContainsKey(symbol)) {
                    first = this.firsts[symbol];
                    firsts.Remove(symbol);
                }
                // if it hasn't beeen seen before, make a brand new first set
                else first = new HashSet<char>();

                // now iterate over each rule for this non-terminal
                foreach (string rule in this.productions[symbol]) {
                    // keep recursively calling this function on the first character of every rule, 
                    // and then add the resulting set to the current set.

                    // if the first symbol is the same as this, skip this string
                    // this is to eliminate infinite recursive calls in case of left recursive grammar.
                    if (rule[0] == symbol) continue;

                    // if the symbol it is called on is a non terminal, whose first set contains ε, 
                    // then add its result to the current set and call 
                    // this function on the next character of the rule as well.
                    for (int i = 0; i < rule.Length; i++) {
                        firstTemp = this.First(rule[i]);
                        first.UnionWith(firstTemp);
                        if (!firstTemp.Contains('ε')) break;
                    }
                }
            }
            return first;
        }

        public void LeftFactor () {
            Dictionary<char, HashSet<string>> temp = new Dictionary<char, HashSet<string>>(productions);
            foreach (KeyValuePair<char, HashSet<string>> production in productions) {
                string[] array = production.Value.ToArray<string>();
                Array.Sort(array, StringComparer.Ordinal);
                string prefix = string.Empty, carry = string.Empty, lastSeen = array[0];
                int count = 0;
                foreach (string str in array) Console.WriteLine(str);
                for (int i = 1; i < array.Length; i++) {
                    prefix = CommonPrefix(array[i], lastSeen);
                    Console.WriteLine("a: " + array[i] + ", p: " + prefix + ", c: " + carry + ", l: " + lastSeen);
                    if (!carry.Equals(prefix)) {
                        count++;
                        if (prefix.Equals(String.Empty)) {
                            lastSeen = array[i];
                            // add new production
                            Console.WriteLine("!------------- " + production.Key + " -> " + carry);
                            count = 0;
                        }
                        if (carry.Length >= prefix.Length) {
                            lastSeen = prefix;
                        }
                    } else lastSeen = array[i];
                    carry = prefix;
                }
                if (!prefix.Equals(String.Empty)) {
                    // add new production
                }
                /*
                 * "qab"
                 * "qac"
                 * "qacd"
                 */
            }
        }

        public string CommonPrefix (string one, string two) {
            var min = Math.Min(one.Length, two.Length);
            string sb = string.Empty;
            for (int i = 0; i < min && one[i] == two[i]; i++) sb = sb + one[i];
            return sb;
        }

        public HashSet<string> GetRuleSet (char nonTerminal) {
            return this.productions[nonTerminal];
        }

        public char[] GetNonTerminalArray () {
            return productions.Keys.ToArray();
        }

        public HashSet<char> GetFirstSet (char symbol) {
            if (firsts.ContainsKey(symbol)) return firsts[symbol];
            else return First(symbol);
        }

        public HashSet<char> GetFollowSet (char symbol) {
            return follows[symbol];
        }

        public HashSet<char> GetSymbols () {
            return this.symbols;
        }

        public HashSet<char> GetTerminals () {
            return this.terminals;
        }

        public HashSet<char> GetNonTerminals () {
            return this.nonTerminals;
        }

        public char GetStartingSymbol () {
            return this.startingSymbol;
        }

        public bool AddProduction (char lhs, HashSet<string> handles) {
            // if we have seen the LHS of this production before, get set of handles associated with it currently
            // otherwise make a new set of handles.
            if (productions.ContainsKey(lhs)) {
                handles.UnionWith(this.productions[lhs]);
                productions.Remove(lhs);
            }

            // add lhs to the set of non-terminals
            this.nonTerminals.Add(lhs);

            // add all handles in the current line into it, and then add it to the dictionary of all productions
            productions.Add(lhs, handles);

            // now start with setting the ordering
            HashSet<KeyValuePair<string, int>> set;
            foreach (string handle in handles) {
                set = new HashSet<KeyValuePair<string, int>>();
                if (ordering.ContainsKey(handle)) {
                    set.UnionWith(ordering[handle]);
                }

                set.Add(new KeyValuePair<string, int>(lhs.ToString(), this.counter++));
                ordering.Add(handle, set);
            }
            return true;
        }

        public bool AddProduction (char lhs, string[] handles) {
            HashSet<string> temp = new HashSet<string>();
            foreach (string handle in handles) temp.Add(handle);
            return this.AddProduction(lhs, temp);
        }

        public bool IsNonTerminal (char nonT) {
            if (this.nonTerminals.Contains(nonT)) return true;
            else return false;
        }
        public bool IsTerminal (char t) {
            if (this.terminals.Contains(t)) return true;
            else return false;
        }

        public int GetProductionNumber (char lhs, string handle) {
            if (ordering.ContainsKey(handle)) {
                foreach (KeyValuePair<string, int> production in ordering[handle]) {
                    if (production.Key.Equals(lhs.ToString())) return production.Value;
                }
            }
            return -1;
        }
    }
}
