using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;

namespace BypassMutex
{
    internal class Program
    {
        static List<Process> ProMult { get; set; } = new ();

        static void Main(string[] args)
        {
            var name = args.FirstOrDefault();


            GetProcessPorExByProssName(name!);
            Console.WriteLine($"{ProMult.Count}");
            var p = ProMult.First();
            if (ProMult.Count > 0 && p is not null)
            {
                var list = HandleModle.GetHandles(p);
                list.ForEach(h => HandleModle.FindAndCloseWeChatMutexHandle(h, p));
            }
        }


        private static int GetProcessPorExByProssName(string ProssName)
        {
            ProMult.Clear();
            Process[] pp = Process.GetProcessesByName(ProssName);
            for (int i = 0; i < pp.Length; i++)
            {
                ProMult.Add(pp[i]);
            }
            return ProMult.Count;
        }
    }
}