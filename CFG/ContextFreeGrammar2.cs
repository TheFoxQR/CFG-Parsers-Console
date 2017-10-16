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

            public string Resolve (int reference) {
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

            public int Resolve (string symbol) {
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
                string[] symbols = terminals.GetBackwardKeys().Concat<string>(nonTerminals.GetBackwardKeys()).ToArray<string>();
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

            public string[] GetTerminals () {
                return terminals.GetBackwardKeys();
            }

            public string[] GetSymbols () {
                return terminals.GetBackwardKeys().Concat(nonTerminals.GetBackwardKeys()).ToArray<string>();
            }
        }

        private class Productions
        {
            // the first int represents lhs, the key in the key value pair is the order number of the handle, 
            // while the int[] represents the actual handle
            Dictionary<int, HashSet<KeyValuePair<int, int[]>>> rules = new Dictionary<int, HashSet<KeyValuePair<int, int[]>>>();
            ReferenceTable refTable;
            private int order_count = 0;

            public Productions (ReferenceTable refTable) {
                this.refTable = refTable;
            }

            // method to add a single lhs -> handle production
            public void AddProduction (String lhs, String handle) {
                int lhs_ref = refTable.Resolve(lhs);
                int[] handle_ref = Resolve(handle);

                //Console.Write(lhs_ref + " -> [ ");
                //foreach (int reference in handle_ref) {
                //    Console.Write(reference + ", ");
                //}
                //Console.WriteLine("]");

                HashSet<KeyValuePair<int, int[]>> handles = new HashSet<KeyValuePair<int, int[]>>();
                if (rules.ContainsKey(lhs_ref)) {
                    handles.UnionWith(this.rules[lhs_ref]);
                    rules.Remove(lhs_ref);
                }

                refTable.SortAsNonTerminal(lhs_ref);
                handles.Add(new KeyValuePair<int, int[]>(order_count++, handle_ref));
                rules.Add(lhs_ref, handles);
            }

            // method to add an lhs -> multiple handles production
            public void AddProductions (String lhs, String[] handles) {
                int lhs_ref = refTable.Resolve(lhs);

                HashSet<int[]> handle_refs = new HashSet<int[]>();
                foreach (String handle in handles) handle_refs.Add(Resolve(handle));

                //foreach (int[] handle_ref in handle_refs) {
                //    Console.Write(lhs_ref + " -> [ ");
                //    foreach (int reference in handle_ref) {
                //        Console.Write(reference + ", ");
                //    }
                //    Console.WriteLine("]");
                //}

                HashSet<KeyValuePair<int, int[]>> new_handles = new HashSet<KeyValuePair<int, int[]>>();
                if (rules.ContainsKey(lhs_ref)) {
                    new_handles.UnionWith(this.rules[lhs_ref]);
                    rules.Remove(lhs_ref);
                }

                refTable.SortAsNonTerminal(lhs_ref);
                foreach (int[] handle_ref in handle_refs) new_handles.Add(new KeyValuePair<int, int[]>(order_count++, handle_ref));
                rules.Add(lhs_ref, new_handles);
            }

            // method that takes a full handle, breaks it down into lexemes, and then uses the
            // reference table to resolve each symbol into its reference.
            private int[] Resolve (String handle) {
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
                for (int i = 0; i < tokens.Length; i++) resolved[i] = refTable.Resolve(tokens[i]);

                return resolved;
            }

            private String Resolve (int reference) {
                return refTable.Resolve(reference);
            }

            private String Resolve (int[] handle_ref) {
                StringBuilder handle = new StringBuilder(handle_ref.Length);
                foreach (int reference in handle_ref) handle.Append(refTable.Resolve(reference));
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
        }

        static private ReferenceTable symbols = new ReferenceTable();
        private Productions productions = new Productions(symbols);

        public ContextFreeGrammar2 (string filepath) {
            FileStream fileStream = new FileStream(filepath, FileMode.Open);
            StreamReader reader = new StreamReader(fileStream);
            this.ReadSymbols(reader);
            this.ReadGrammar(reader);

            // sort all unsorted symbols as terminals.
            Dictionary<int, string> dict = new Dictionary<int, string>(symbols.GetUnsorted());
            foreach (KeyValuePair<int, string> item in dict) symbols.SortAsTerminal(item.Key);

            Console.Write("NonTerminals: [ ");
            foreach (string symbol in symbols.GetNonTerminals()) {
                Console.Write(symbol + " (" + symbols.Resolve(symbol) + "), ");
            }
            Console.WriteLine("]");
            Console.Write("   Terminals: [ ");
            foreach (string symbol in symbols.GetTerminals()) {
                Console.Write(symbol + " (" + symbols.Resolve(symbol) + "), ");
            }
            Console.WriteLine("]");
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
    }
}
