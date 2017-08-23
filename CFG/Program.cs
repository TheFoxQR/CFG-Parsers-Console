using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

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

        public ContextFreeGrammar(string filepath)
        {
            string line;
            FileStream fileStream = new FileStream(filepath, FileMode.Open);
            HashSet<string> set;

            using (StreamReader reader = new StreamReader(fileStream))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    string[] components = line.Replace(" ", String.Empty).Split(new[] {"->", "|"}, StringSplitOptions.None);
                    nonTerminals.Add(components[0][0]);
                    foreach (string str in components) foreach (char c in str) symbols.Add(c);
                    if (productions.ContainsKey(components[0][0]))
                    {
                        set = this.productions[components[0][0]];
                        productions.Remove(components[0][0]);
                    }
                    else
                    {
                        set = new HashSet<string>();
                    }
                    for (int i = 1; i < components.Length; i++) set.Add(components[i]);
                    productions.Add(components[0][0], set);
                }
            }

            terminals.UnionWith(symbols);
            symbols.UnionWith(nonTerminals);
            terminals.ExceptWith(nonTerminals);

            //Console.Write("Symbols:       {");
            //foreach (char c in symbols) Console.Write(" " + c + " ");
            //Console.WriteLine("}");
            //Console.Write("Terminals:     {");
            //foreach (char c in terminals) Console.Write(" " + c + " ");
            //Console.WriteLine("}");
            //Console.Write("Non-Terminals: {");
            //foreach (char c in nonTerminals) Console.Write(" " + c + " ");
            //Console.WriteLine("}\n");

            // find firsts
            Dictionary<char, HashSet<char>> firstsTemp;
            do
            {
                //Console.WriteLine("\nDoing one pass on firsts...");
                firstsTemp = new Dictionary<char, HashSet<char>>(firsts);
                foreach (char symbol in nonTerminals)
                {
                    firsts.Add(symbol, First(symbol));
                    //Console.Write(symbol + " to {");
                    //foreach (char sym in firsts[symbol]) Console.Write(" " + sym + " ");
                    //Console.WriteLine("}");
                }
            } while (!CompareFirsts(firsts, firstsTemp));
            //Console.WriteLine("\n");
        }

        private bool CompareFirsts(Dictionary<char, HashSet<char>> one, Dictionary<char, HashSet<char>> two)
        {
            if (one.Count() != two.Count()) return false;
            foreach (KeyValuePair<char, HashSet<char>> entry in one) if (!two.ContainsKey(entry.Key) || !two[entry.Key].IsSubsetOf(entry.Value) || !entry.Value.IsSubsetOf(two[entry.Key])) return false;
            return true;
        }

        private HashSet<char> First(char symbol)
        {
            HashSet<char> first, firstTemp;
            // if not a non-terminal character, the first set is a singleton set containing the character itself.
            if (!nonTerminals.Contains(symbol))
            {
                first = new HashSet<char>();
                first.Add(symbol);
            }
            else
            {
                // if this non terminal has already been dealt with before, get its current first set.
                if (this.firsts.ContainsKey(symbol))
                {
                    first = this.firsts[symbol];
                    firsts.Remove(symbol);
                }
                // if it hasn't beeen seen before, make a brand new first set
                else first = new HashSet<char>();

                // now iterate over each rule for this non-terminal
                foreach (string rule in this.productions[symbol])
                {
                    // keep recursively calling this function on the first character of every rule, and then add the resulting set to the current set.
                    // if the symbol it is called on is a non terminal, whose first set contains ε, then add its result to the current set and call this function on the next character of the rule as well.
                    for (int i = 0; i < rule.Length; i++)
                    {
                        firstTemp = this.First(rule[i]);
                        first.UnionWith(firstTemp);
                        if (!firstTemp.Contains('ε')) break;
                    }
                    // old loop
                    //int i = 0;
                    //while (true)
                    //{
                    //    firstTemp = this.First(rule[i]);
                    //    first.UnionWith(firstTemp);
                    //    if (!firstTemp.Contains('ε') || i >= rule.Length - 1) break;
                    //    else i++;
                    //}
                }
            }
            return first;
        }

        public HashSet<string> GetRuleSet(char nonTerminal)
        {
            return this.productions[nonTerminal];
        }

        public char[] GetNonTerminalArray()
        {
            return productions.Keys.ToArray();
        }

        public HashSet<char> GetFirstSet(char symbol)
        {
            if (firsts.ContainsKey(symbol)) return firsts[symbol];
            else return First(symbol);
        }

        public HashSet<char> GetSymbols()
        {
            return this.symbols;
        }

        public HashSet<char> GetTerminals()
        {
            return this.terminals;
        }

        public HashSet<char> GetNonTerminals()
        {
            return this.nonTerminals;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            ContextFreeGrammar g = new ContextFreeGrammar("grammar.txt");
            Console.WriteLine("Grammar breakdown:- \n");
            foreach (char nonTerminal in g.GetNonTerminals()) {
                Console.Write(nonTerminal + " to {");
                foreach (string rule in g.GetRuleSet(nonTerminal)) Console.Write(" " + rule + " ");
                Console.WriteLine("}");
            }
            Console.WriteLine("\n");

            // Console.WriteLine("First Sets:- ");
            foreach (char symbol in g.GetNonTerminals())
            {
                Console.Write("FIRST(" + symbol + ") = {");
                foreach (char sym in g.GetFirstSet(symbol)) Console.Write(" " + sym + " ");
                Console.WriteLine("}");
            }
            Console.WriteLine("\n");

            // Suspend the screen.  
            System.Console.ReadLine();
        }
    }
}