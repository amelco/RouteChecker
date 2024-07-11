namespace RouteChecker
{
    public class Args
    {
        public string CaminhoArquivo { get; set; } = "";
        public bool MostrarResultado { get; set; } = false;

        public Args(string[] args)
        {
            if (args.Length == 0)
            {
                MostraUso();
                Do.Kill();
            }
            CaminhoArquivo = args[0];
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "-r")
                {
                    MostrarResultado = true;
                }
                else
                {
                    Console.WriteLine("Opção inválida: " + args[i]);
                    MostraUso();
                    Do.Kill();
                }
            }
        }

        private static void MostraUso()
        {
            var nome = AppDomain.CurrentDomain.FriendlyName;
            Console.WriteLine("Uso: " + nome + " <arquivo> [OPÇÕES]");
            Console.WriteLine("     arquivo:  arquivo http a ser executado (obrigatório)");
            Console.WriteLine("     -r:       mostra resultado das requisições");
        }
    }
}
