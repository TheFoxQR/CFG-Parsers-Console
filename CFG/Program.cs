using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace CFG
{
    class Program
    {
        static void Main(string[] args) {
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            ContextFreeGrammar2 g = new ContextFreeGrammar2("grammar.txt");
            //Console.WriteLine("Grammar breakdown:- \n");
            //foreach (char nonTerminal in g.GetNonTerminals()) {
            //    Console.Write(nonTerminal + " to {");
            //    foreach (string rule in g.GetRuleSet(nonTerminal)) Console.Write(" " + rule + " ");
            //    Console.WriteLine("}");
            //}
            //Console.WriteLine("\n");
            //Console.WriteLine("Starting Symbol: " + g.GetStartingSymbol() + "\n");

            //Console.WriteLine("First Sets:- ");
            //foreach (char symbol in g.GetNonTerminals()) {
            //    Console.Write("FIRST(" + symbol + ") = {");
            //    foreach (char sym in g.GetFirstSet(symbol)) Console.Write(" " + sym + " ");
            //    Console.WriteLine("}");
            //}
            //Console.WriteLine("\n");

            //Console.WriteLine("Follow Sets:- ");
            ////foreach (char symbol in g.GetTerminals()) {
            ////    Console.Write("FOLLOW(" + symbol + ") = {");
            ////    foreach (char sym in g.GetFollowSet(symbol)) Console.Write(" " + sym + " ");
            ////    Console.WriteLine("}");
            ////}
            //foreach (char symbol in g.GetNonTerminals()) {
            //    Console.Write("FOLLOW(" + symbol + ") = {");
            //    foreach (char sym in g.GetFollowSet(symbol)) Console.Write(" " + sym + " ");
            //    Console.WriteLine("}");
            //}
            //Console.WriteLine("\n");

            //Console.WriteLine("Making the Canonical collection of LR(0) itemsets...\n");
            //SimpleLR slr = new SimpleLR(g);

            //foreach (KeyValuePair<int, HashSet<KeyValuePair<char, KeyValuePair<int, string>>>> state in slr.GetCanonicalCollection()) {
            //    Console.Write("I" + state.Key + ": ");
            //    bool tab = false;
            //    foreach (KeyValuePair<char, KeyValuePair<int, string>> item in state.Value) {
            //        if (tab) Console.Write((((state.Key / 10) == 0) ? "    " : "     "));
            //        Console.WriteLine(item.Key + " -> " + item.Value.Value.Substring(0, item.Value.Key) + "." + item.Value.Value.Substring(item.Value.Key));
            //        tab = true;
            //    }
            //    Console.WriteLine();
            //}
            //Console.WriteLine("\n");

            //string[,] parseTable = slr.GetParsingTable();
            //Console.WriteLine("Making SLR Parsing Table... \n");

            //Console.Write(" ++====++");
            //for (int i = 0; i < parseTable.GetLength(1); i++) {
            //    Console.Write("=========+");
            //}
            //Console.WriteLine("+");
            //Console.Write(" ||    |");
            //for (int i = 0; i < parseTable.GetLength(1); i++) {
            //    Console.Write(String.Format("|{0, 8} ", slr.GetParsingTableColumnTag(i)));
            //}
            //Console.WriteLine("||");
            //Console.Write(" ++====++");
            //for (int i = 0; i < parseTable.GetLength(1); i++) {
            //    Console.Write("=========+");
            //}
            //Console.WriteLine("+");

            //for (int j = 0; j < parseTable.GetLength(0); j++) {
            //    Console.Write(String.Format(" ||{0, 3} |", j));
            //    for (int i = 0; i < parseTable.GetLength(1); i++) {
            //        Console.Write(String.Format("|{0, 8} ", parseTable[j,i]));
            //    }
            //    Console.WriteLine("||");
            //}
            //Console.Write(" ++====++");
            //for (int i = 0; i < parseTable.GetLength(1); i++) {
            //    Console.Write("=========+");
            //}
            //Console.WriteLine("+");

            //// Suspend the screen.  
            System.Console.ReadLine();
        }
    }
}