namespace RouteChecker
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Args argumentos = new(args);

            Parser app = new(argumentos.CaminhoArquivo);
            var rotas = app.Parse();

            foreach (var rota in rotas)
            {
                // TODO(Andre - 11/07/2024): Não passar as variáveis para cada rota. Cada rota deve ter SOMENTE a lista de variáveis que ela utiliza.
                rota.FazRequisicao(app.Variaveis);
                if (rota.RespostaCodigo is null) Do.Kill("Erro. Rota não retornou resposta.");

                var corTexto = Console.ForegroundColor;
                if (rota.RespostaCodigo < 200 || rota.RespostaCodigo >= 300)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }

                Console.WriteLine($"({rota.RespostaCodigo})");
                Console.ForegroundColor = corTexto;
                if (argumentos.MostrarResultado) Console.WriteLine(rota.RespostaBody);
            }
        }
    }
}
