using System;
using System.IO;

namespace CFG
{
    class Program
    {
        static void Main(string[] args)
        {
            int counter = 0;
            string line;

            // Read the file and display it line by line.  
            FileStream fileStream = new FileStream("grammar.txt", FileMode.Open);
            using (StreamReader reader = new StreamReader(fileStream))
            {
                while ((line = reader.ReadLine()) != null)
                {
                    Console.WriteLine(line);
                    counter++;
                }
            }
            System.Console.WriteLine("There were {0} lines.", counter);
            // Suspend the screen.  
            System.Console.ReadLine();
        }
    }
}