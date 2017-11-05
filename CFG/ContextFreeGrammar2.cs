using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CFG
{
    class ContextFreeGrammar2
    {
        private class ReferenceTable
        {
            private TwoWayDictionary<int, string> unsorted = new TwoWayDictionary<int, string>();
            private TwoWayDictionary<int, string> nonTerminals = new TwoWayDictionary<int, string>();
            private TwoWayDictionary<int, string> terminals = new TwoWayDictionary<int, string>();
            private int ref_count = 1;

            public ReferenceTable () {
                terminals.Add(ref_count++, "ε");
                terminals.Add(ref_count++, "$");
                nonTerminals.Add(ref_count++, "Θ");
            }

            public void MakeInitialReferences (string[] symbols) {
                // foreach (string str in symbols) Console.WriteLine(str);
                foreach (string symbol in symbols) unsorted.Add(ref_count++, symbol);
            }

            public bool AddTerminal(string symbol) {
                if (unsorted.ContainsBackwardKey(symbol) || terminals.ContainsBackwardKey(symbol) || nonTerminals.ContainsBackwardKey(symbol)) return false;
                else terminals.Add(ref_count++, symbol);
                return true;
            }

            public bool AddNonTerminal (string symbol) {
                if (unsorted.ContainsBackwardKey(symbol) || terminals.ContainsBackwardKey(symbol) || nonTerminals.ContainsBackwardKey(symbol)) return false;
                else nonTerminals.Add(ref_count++, symbol);
                return true;
            }

            public string Lookup (int reference) {
                string symbol = String.Empty;
                if (unsorted.ContainsForwardKey(reference)) {
                    unsorted.ForwardSearch(reference, out symbol);
                    return symbol;
                }
                if (terminals.ContainsForwardKey(reference)) {
                    terminals.ForwardSearch(reference, out symbol);
                    return symbol;
                }
                if (nonTerminals.ContainsForwardKey(reference)) {
                    nonTerminals.ForwardSearch(reference, out symbol);
                    return symbol;
                }
                return symbol;
            }

            public int Lookup (string symbol) {
                int reference = 0;
                if (unsorted.ContainsBackwardKey(symbol)) {
                    unsorted.BackwardSearch(symbol, out reference);
                    return reference;
                }
                if (terminals.ContainsBackwardKey(symbol)) {
                    terminals.BackwardSearch(symbol, out reference);
                    return reference;
                }
                if (nonTerminals.ContainsBackwardKey(symbol)) {
                    nonTerminals.BackwardSearch(symbol, out reference);
                    return reference;
                }
                return reference;
            }

            public string[] GetLexingArray () {
                string[] symbols = terminals.GetBackwardKeys().Concat(nonTerminals.GetBackwardKeys()).Concat(unsorted.GetBackwardKeys()).ToArray<string>();
                int[] length = new int[symbols.Length];
                for (int i = 0; i < symbols.Length; i++) length[i] = symbols[i].Length;
                Array.Sort(symbols, (x, y) => y.Length.CompareTo(x.Length));
                return symbols;
            }

            public bool SortAsNonTerminal (int reference) {
                bool success = false;
                if (unsorted.ContainsForwardKey(reference)) {
                    unsorted.ForwardSearch(reference, out string value);
                    success = unsorted.Remove(reference, value);
                    nonTerminals.Add(reference, value);
                }
                return success;
            }

            public bool SortAsNonTerminal (string value) {
                bool success = false;
                if (unsorted.ContainsBackwardKey(value)) {
                    unsorted.BackwardSearch(value, out int reference);
                    success = unsorted.Remove(reference, value);
                    nonTerminals.Add(reference, value);
                }
                return success;
            }

            public bool SortAsTerminal (int reference) {
                bool success = false;
                if (unsorted.ContainsForwardKey(reference)) {
                    unsorted.ForwardSearch(reference, out string value);
                    success = unsorted.Remove(reference, value);
                    terminals.Add(reference, value);
                }
                return success;
            }

            public bool SortAsTerminal (string value) {
                bool success = false;
                if (unsorted.ContainsBackwardKey(value)) {
                    unsorted.BackwardSearch(value, out int reference);
                    success = unsorted.Remove(reference, value);
                    terminals.Add(reference, value);
                }
                return success;
            }

            public Dictionary<int, string> GetUnsorted () {
                return unsorted.GetForwardDictionary();
            }

            public string[] GetNonTerminals () {
                return nonTerminals.GetBackwardKeys();
            }

            public int[] GetNonTerminalReferences () {
                return nonTerminals.GetForwardKeys();
            }

            public string[] GetTerminals () {
                return terminals.GetBackwardKeys();
            }

            public int[] GetTerminalReferences () {
                return terminals.GetForwardKeys();
            }

            public string[] GetSymbols () {
                return terminals.GetBackwardKeys().Concat(nonTerminals.GetBackwardKeys()).ToArray<string>();
            }

            public int[] GetSymbolReferences () {
                return terminals.GetForwardKeys().Concat(nonTerminals.GetForwardKeys()).ToArray<int>();
            }
        }

        private class Productions
        {
            // the first int represents lhs, the key in the key value pair is the order number of the handle, 
            // while the int[] represents the actual handle
            Dictionary<int, HashSet<KeyValuePair<int, int[]>>> rules = new Dictionary<int, HashSet<KeyValuePair<int, int[]>>>();
            ReferenceTable refTable;
            private int sequenceNum = 0;
            bool print = false;

            public IEnumerable<int[]> this[int lhs_ref] {
                get { return rules[lhs_ref].Select(kvp => kvp.Value); }
                set { AddProductions(lhs_ref, value.ToArray<int[]>()); }
            }

            public IEnumerable<int[]> GetRHS (string lhs) {
                return this[refTable.Lookup(lhs)];
            }

            public Productions (ReferenceTable refTable) {
                this.refTable = refTable;
            }

            // method to add a single lhs -> handle production
            public void AddProduction (String lhs, String handle) {
                int lhs_ref = refTable.Lookup(lhs);
                int[] handle_ref = Lex(handle);

                if (print) {
                    Console.Write(lhs_ref + "(" + lhs + ") -> [ ");
                    foreach (int reference in handle_ref) {
                        Console.Write(reference + ", ");
                    }
                    Console.WriteLine("] (" + handle + ")");
                }

                HashSet<KeyValuePair<int, int[]>> handles = new HashSet<KeyValuePair<int, int[]>>();
                if (rules.ContainsKey(lhs_ref)) {
                    handles.UnionWith(this.rules[lhs_ref]);
                    rules.Remove(lhs_ref);
                }

                refTable.SortAsNonTerminal(lhs_ref);
                handles.Add(new KeyValuePair<int, int[]>(sequenceNum++, handle_ref));
                rules.Add(lhs_ref, handles);
            }

            public void AddProduction (int lhs_ref, int[] handle_ref) {
                if (print) {
                    Console.Write(lhs_ref + "(" + refTable.Lookup(lhs_ref) + ") -> [ ");
                    foreach (int reference in handle_ref) {
                        Console.Write(reference + ", ");
                    }
                    Console.WriteLine("] (" + this.Resolve(handle_ref) + ")");
                }

                HashSet<KeyValuePair<int, int[]>> handles = new HashSet<KeyValuePair<int, int[]>>();
                if (rules.ContainsKey(lhs_ref)) {
                    handles.UnionWith(this.rules[lhs_ref]);
                    rules.Remove(lhs_ref);
                }

                refTable.SortAsNonTerminal(lhs_ref);
                handles.Add(new KeyValuePair<int, int[]>(sequenceNum++, handle_ref));
                rules.Add(lhs_ref, handles);
            }

            // method to add an lhs -> multiple handles production
            public void AddProductions (String lhs, String[] handles) {
                int lhs_ref = refTable.Lookup(lhs);

                HashSet<int[]> handle_refs = new HashSet<int[]>();
                foreach (String handle in handles) handle_refs.Add(Lex(handle));

                if (print) {
                    foreach (int[] handle_ref in handle_refs) {
                        Console.Write(lhs_ref + " -> [ ");
                        foreach (int reference in handle_ref) {
                            Console.Write(reference + ", ");
                        }
                        Console.WriteLine("]");
                    }
                }

                HashSet<KeyValuePair<int, int[]>> new_handles = new HashSet<KeyValuePair<int, int[]>>();
                if (rules.ContainsKey(lhs_ref)) {
                    new_handles.UnionWith(this.rules[lhs_ref]);
                    rules.Remove(lhs_ref);
                }

                refTable.SortAsNonTerminal(lhs_ref);
                foreach (int[] handle_ref in handle_refs) new_handles.Add(new KeyValuePair<int, int[]>(sequenceNum++, handle_ref));
                rules.Add(lhs_ref, new_handles);
            }

            public void RemoveProduction (int[] handle_ref) {
                this.RemoveProduction(GetLHS(handle_ref), handle_ref);
            }

            public void RemoveProduction (int lhs_ref, int[] handle_ref) {
                rules[lhs_ref].RemoveWhere(kvp => kvp.Value == handle_ref);
            }

            public void AddProductions (int lhs_ref, int[][] handle_refs) {
                if (print) {
                    foreach (int[] handle_ref in handle_refs) {
                        Console.Write(lhs_ref + " -> [ ");
                        foreach (int reference in handle_ref) {
                            Console.Write(reference + ", ");
                        }
                        Console.WriteLine("]");
                    }
                }

                HashSet<KeyValuePair<int, int[]>> new_handles = new HashSet<KeyValuePair<int, int[]>>();
                if (rules.ContainsKey(lhs_ref)) {
                    new_handles.UnionWith(this.rules[lhs_ref]);
                    rules.Remove(lhs_ref);
                }

                refTable.SortAsNonTerminal(lhs_ref);
                foreach (int[] handle_ref in handle_refs) new_handles.Add(new KeyValuePair<int, int[]>(sequenceNum++, handle_ref));
                rules.Add(lhs_ref, new_handles);
            }


            // method that takes a full handle, breaks it down into lexemes, and then uses the
            // reference table to resolve each symbol into its reference.
            private int[] Lex (String handle) {
                StringBuilder new_handle = new StringBuilder(handle);

                foreach (String symbol in refTable.GetLexingArray()) {
                    // 'ˀ' 'ˁ'
                    String temp = String.Empty;
                    int index = -2;
                    while ((index = IndexOfUnmarked((temp = new_handle.ToString()), symbol)) != -1) {
                        new_handle.Insert(index + symbol.Length, 'ˀ');
                        new_handle.Insert(index, 'ˁ');
                    }
                }
                // Console.WriteLine(new_handle.ToString());

                String[] tokens = new_handle.ToString().Split(new[] { "ˁ", "ˀ" }, StringSplitOptions.RemoveEmptyEntries);
                int[] resolved = new int[tokens.Length];
                for (int i = 0; i < tokens.Length; i++) resolved[i] = refTable.Lookup(tokens[i]);

                return resolved;
            }

            public String Resolve (int[] handle_ref) {
                StringBuilder handle = new StringBuilder(handle_ref.Length);
                foreach (int reference in handle_ref) handle.Append(refTable.Lookup(reference));
                return handle.ToString();
            }

            private bool ContainsUnmarked (string str, string substr) {
                bool found = false;
                int state = 0;
                for (int i = 0; i < str.Length - substr.Length; i++) {
                    if (state == 1) {
                        if (str[i] == 'ˀ') state = 0;
                        continue;
                    }
                    if (str[i] == 'ˁ') {
                        state = 1;
                        continue;
                    }
                    if (str.Substring(i, substr.Length).Equals(substr)) found = true;
                }
                return found;
            }

            private int IndexOfUnmarked (string str, string substr) {
                int index = -1;
                bool state = false;
                for (int i = 0; i <= str.Length - substr.Length; i++) {
                    if (state == true) {
                        if (str[i] == 'ˀ') state = false;
                        continue;
                    }
                    if (str[i] == 'ˁ') {
                        state = true;
                        continue;
                    }
                    string strx = str.Substring(i, substr.Length);
                    if (str.Substring(i, substr.Length).Equals(substr)) index = i;
                }
                return index;
            }

            // !!-- optimization needed.
            public int GetLHS(int[] handle_ref) {
                foreach (int lhs_ref in rules.Keys) {
                    foreach (var kvp in rules[lhs_ref]) {
                        if (kvp.Value.SequenceEqual(handle_ref)) return lhs_ref;
                    }
                }
                return -1;
            }

            public int GetSequenceNumber (int lhs_ref, int[] handle_ref) {
                return rules[lhs_ref].Where(kvp => kvp.Value.SequenceEqual(handle_ref)).Select(kvp => kvp.Key).Single();
            }

            public IEnumerable<int[]> GetAllHandleReferences () {
                return rules.Values.SelectMany(hashset => hashset.Select(kvp => kvp.Value));
            }

            public IEnumerable<KeyValuePair<int, int[]>> GetAllRules () {
                return from reference in rules.Keys
                       from kvp2 in rules[reference]
                       select new KeyValuePair<int, int[]>(reference, kvp2.Value);
            }

            public IEnumerable<string> GetAllHandles () {
                List<string> handles = new List<string>();
                foreach (int[] handle_ref in GetAllHandleReferences()) handles.Add(this.Resolve(handle_ref));
                return handles;
            }
        }

        bool print = false;
        static private ReferenceTable symbols = new ReferenceTable();
        private Productions productions = new Productions(symbols);
        private MultiValueDictionary<int, int> firstSet = new MultiValueDictionary<int, int>();
        private MultiValueDictionary<int, int> followSet = new MultiValueDictionary<int, int>();

        public ContextFreeGrammar2 (string filepath) {
            FileStream fileStream = new FileStream(filepath, FileMode.Open);
            StreamReader reader = new StreamReader(fileStream);
            this.ReadSymbols(reader);
            this.ReadGrammar(reader);

            // sort all unsorted symbols as terminals.
            Dictionary<int, string> dict = new Dictionary<int, string>(symbols.GetUnsorted());
            foreach (KeyValuePair<int, string> item in dict) symbols.SortAsTerminal(item.Key);

            if (print) {
                Console.Write("NonTerminals: [ ");
                foreach (string symbol in symbols.GetNonTerminals()) {
                    Console.Write(symbol + " (" + symbols.Lookup(symbol) + "), ");
                }
                Console.WriteLine("]");
                Console.Write("   Terminals: [ ");
                foreach (string symbol in symbols.GetTerminals()) {
                    Console.Write(symbol + " (" + symbols.Lookup(symbol) + "), ");
                }
                Console.WriteLine("]");
            }

            // LeftFactor();
        }

        private void ReadSymbols (StreamReader reader) {
            string line;
            HashSet<String> global_symbol_list = new HashSet<String>();
            while ((line = reader.ReadLine()) != null) {
                string[] local_symbol_list = line.Replace(" ", String.Empty).Split(new[] { ",", "." }, StringSplitOptions.RemoveEmptyEntries);
                if (line == String.Empty || (local_symbol_list[0][0] == '/' && local_symbol_list[0][1] == '/')) continue;
                foreach (string token in local_symbol_list) global_symbol_list.Add(token);
                // global_symbol_list.UnionWith(local_symbol_list);
                if (line != String.Empty && line[line.Length - 1] == '.') break;
            }
            symbols.MakeInitialReferences(global_symbol_list.ToArray<String>());
        }

        private void ReadGrammar (StreamReader reader) {
            string line;
            while ((line = reader.ReadLine()) != null) {
                string[] components = line.Replace(" ", String.Empty).Split(new[] { "->", "|" }, StringSplitOptions.RemoveEmptyEntries);
                if (line == String.Empty || (components[0][0] == '/' && components[0][1] == '/')) continue;
                productions.AddProductions(components[0], components.Skip(1).ToArray<string>());
            }
        }

        private bool CompareIntegerArray(int[] array1, int[] array2) {
            int i = 0;
            if (array1.Length == 0 || array2.Length == 0) return false;
            for (i = 0; (i < ((array1.Length < array2.Length ? array1.Length : array2.Length) - 1)) && (array2[i] == array1[i]); i++) ;
            if ((i == array2.Length) && (array2.Length > array1.Length)) return true;
            if (array2[i] > array1[i]) return true;
            return false;
        }

        private int[] CommonPrefix (int[] array1, int[] array2) {
            List<int> prefix = new List<int>();
            for (int i = 0; (i < (array1.Length < array2.Length ? array1.Length : array2.Length)) && (array2[i] == array1[i]); i++) prefix.Add(array1[i]);
            return prefix.ToArray();
        }

        // !!-- needs optimization.
        /// <summary>
        /// top level:- 
        /// while (new productions are left factored)
        ///     get all handles
        ///     sort according to lhs
        ///     go through the list left factoring as you go
        /// 
        /// left factoring the list algorithm,
        /// four components: currently common prefix, carry prefix, last seen value, and the number of handles having this prefix as common
        /// currently common prefix is the common prefix between the value used in the common prefix check last time (lastseen) vs current value
        /// - if this prefix is different than carried prefix (carry_prefix != curr_prefix) then
        ///     - increment count
        ///     - check the length of curr_prefix. if length = 0, then this means that there is nothing in common between the last seen value and current value.
        ///         thus, carried prefix is the one needed for left factoring, while the count will tell how many previous values have the carried prefix in common.
        ///         - in this case, remove previous count number of productions and add them to an intermediate list.
        ///         - now make one new production with handle length one more then the carried prefix, and lhs same as the lhs of removed productions.
        ///             make new nonterminal, and appending it to the end of the carried prefix will give this new handle.
        ///         - now make new productions having lhs as this new lhs, while the rhs handles are from the intermediate list previously generated.
        ///             This time, remove the carried prefix from each handle.
        ///         - make the currently checked value last seen.
        ///     - if length if curr_prefix is less than carried prefix, then this means that next time you just need to compare with this prefix instead of the full current value.
        ///         in this case, make the last seen value the current prefix.
        /// - if this prefix is same as the carried prefix, then make the last seen value this value. Because there might
        /// - in all these cases, the current prefix becomes carried prefix at the end of the iteration.
        /// </summary>
        private void LeftFactor () {
            int i;// = 0;
            bool change;
            KeyValuePair<int, int[]>[] rules;// = productions.GetAllRules().ToArray<KeyValuePair<int, int[]>>();
            bool productions_added = true;

            while (productions_added) {
                productions_added = false;

                i = 0;
                rules = productions.GetAllRules().ToArray<KeyValuePair<int, int[]>>();
                //foreach (var kvp in rules) Console.WriteLine(" " + symbols.Lookup(kvp.Key) + " => " + productions.Resolve(kvp.Value));
                //Console.WriteLine("\n\n");

                // bubble sorting mess
                // sort handles into alphabetical order
                // sort so that all kvps with same keys are together.
                change = false;
                for (i = 0; i < rules.Length; i++) {
                    change = false;
                    for (int j = 1; j < rules.Length; j++) {
                        if (rules[j].Key == rules[j - 1].Key && this.CompareIntegerArray(rules[j].Value, rules[j - 1].Value)) {
                            var temp = rules[j];
                            rules[j] = rules[j - 1];
                            rules[j - 1] = temp;
                            change = true;
                        }
                    }
                    if (change == false) break;
                }

                // actual algorithm
                i = 0;
                int lhs_ref = 0;
                while (i < rules.Length) {
                    lhs_ref = rules[i].Key;
                    int[] prefix = { }, carry = { }, lastseen = rules[i].Value;
                    int count = 0;
                    //Console.WriteLine("for lhs " + lhs_ref + ",");
                    while (++i < rules.Length && lhs_ref == rules[i].Key) {
                        prefix = CommonPrefix(lastseen, rules[i].Value);

                        if (!carry.SequenceEqual(prefix)) {
                            count++;
                            if (prefix.Length == 0) {
                                lastseen = rules[i].Value;
                                productions_added = true;
                                // make new lhs symbol by appending a ' to the old lhs symbol.
                                string new_lhs = symbols.Lookup(lhs_ref) + "'";
                                // if it already exists, append another ' and try again.
                                while (!symbols.AddNonTerminal(new_lhs)) new_lhs += "'";
                                int new_lhs_ref = symbols.Lookup(new_lhs);
                                List<int[]> removed_handles = new List<int[]>();
                                // remove existing handles with a common prefix and remember them
                                for (int j = i - count; j < i; j++) {
                                    //Console.WriteLine(symbols.Lookup(lhs_ref) + " -> " + productions.Resolve(rules[j].Value));
                                    removed_handles.Add(rules[j].Value);
                                    productions.RemoveProduction(rules[j].Key, rules[j].Value);
                                }
                                // make changes to all the remembered handles, and also make new ones
                                int[] new_handle = new int[carry.Length + 1];
                                for (int j = 0; j < carry.Length; j++) new_handle[j] = carry[j];
                                new_handle[carry.Length] = new_lhs_ref;
                                productions.AddProduction(lhs_ref, new_handle);
                                for (int j = 0; j < removed_handles.Count; j++) {
                                    if (removed_handles[j].Length - carry.Length != 0) {
                                        new_handle = new int[removed_handles[j].Length - carry.Length];
                                        for (int k = carry.Length; k < removed_handles[j].Length; k++) new_handle[k - carry.Length] = removed_handles[j][k];
                                    }
                                    else new_handle = new int[] { symbols.Lookup("ε") };
                                    productions.AddProduction(new_lhs_ref, new_handle);
                                }
                                //Console.WriteLine("Common prefix: " + productions.Resolve(carry));
                                count = 0;
                            }
                            if (prefix.Length < carry.Length) {
                                lastseen = prefix;
                            }
                        } else lastseen = rules[i].Value;
                        carry = prefix;
                    }
                    //Console.WriteLine();
                }
                //Console.WriteLine("\n");

            }
            //rules = productions.GetAllRules().ToArray<KeyValuePair<int, int[]>>();
            //change = false;
            //for (i = 0; i < rules.Length; i++) {
            //    change = false;
            //    for (int j = 1; j < rules.Length; j++) {
            //        if (rules[j].Key == rules[j - 1].Key && this.CompareIntegerArray(rules[j].Value, rules[j - 1].Value)) {
            //            var temp = rules[j];
            //            rules[j] = rules[j - 1];
            //            rules[j - 1] = temp;
            //            change = true;
            //        }
            //    }
            //    if (change == false) break;
            //}
            //foreach (var kvp in rules) Console.WriteLine(" " + symbols.Lookup(kvp.Key) + " => " + productions.Resolve(kvp.Value));
        }
    }
}
