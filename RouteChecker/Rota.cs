using System.Text;

namespace RouteChecker
{
    public class Rota
    {
        public string? Metodo { get; set; }
        public string? UrlCrua { get; set; }
        public string? Url { get; set; }
        public Dictionary<string, string> Header { get; set; } = new();
        public string? Body { get; set; }
        public int? RespostaCodigo { get; set; }
        public string? RespostaBody { get; set; }

        public void FazRequisicao(Parser parser)
        {
            Dictionary<string, bool> variaveisDaRota = new();

            if (UrlCrua is null) Do.Kill("Erro. URL da rota não definida.");
            Url = SubstituiVariaveis(UrlCrua!, parser.Variaveis, variaveisDaRota);
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(Metodo!), Url);

            foreach (var header in Header)
            {
                if (header.Key.ToLower() == "content-type") continue; // Content-Type é definido no Body
                string value = SubstituiVariaveis(header.Value, parser.Variaveis, null);
                request.Headers.Add(header.Key, value);
            }

            // por enquanto só aceita json
            if (Body != null)
            {
                request.Content = new StringContent(Body, Encoding.UTF8, "application/json");
            }

            // pergunta por variavel que deve ser inserida antes da requisição
            foreach (var v in variaveisDaRota)
            {
                if (v.Value) // variável que precisa ser inserida
                {
                    var (left, top) = Console.GetCursorPosition();
                    Console.SetCursorPosition(0, top - 1);
                    var corTexto = Console.ForegroundColor;
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.Write($"Insira o valor da variável '{v.Key}': ");
                    Console.ForegroundColor = corTexto;
                    string? resposta = Console.ReadLine();
                    Console.SetCursorPosition(left, top);
                    parser.Variaveis[v.Key] = resposta!;
                    Url = SubstituiVariaveis(UrlCrua!, parser.Variaveis, variaveisDaRota);
                }
            }

            HttpResponseMessage response = client.SendAsync(request).Result;
            RespostaCodigo = (int)response.StatusCode;
            RespostaBody = IdentaJson(response.Content.ReadAsStringAsync().Result);
        }

        private static string SubstituiVariaveis(string linha, Dictionary<string, string> vars, Dictionary<string, bool>? rotaVars)
        {
            string nova_linha = "";
            for (int i = 0; i < linha.Length; i++)
            {
                string nome_var = "";
                string valor_var = "";
                if (linha[i] == '{' && linha[i + 1] == '{')
                {
                    i += 2;
                    while (linha[i] != '}')
                    {
                        nome_var += linha[i];
                        i++;
                    }
                    i++;
                    if (linha[i] != '}')
                    {
                        Do.Kill("Erro de sintaxe na linha: " + linha);
                    }
                    valor_var = vars.FirstOrDefault(v => v.Key == nome_var).Value;
                    if (valor_var is null) Do.Kill($"Não foi possível obter o valor da variável {nome_var}. Verifique se ela foi declarada anteriormente.");

                    nova_linha += valor_var;
                    if (vars.ContainsKey(nome_var))
                        vars[nome_var] = valor_var!;
                    else
                        vars.Add(nome_var, valor_var!);
                    i++;
                    if (rotaVars is not null)
                    {
                        if (rotaVars.ContainsKey(nome_var))
                            rotaVars[nome_var] = valor_var![0] == '?';
                        else
                            rotaVars.Add(nome_var, valor_var![0] == '?');
                    }
                }
                if (i >= linha.Length) break;
                nova_linha += linha[i];
            }
            return nova_linha == "" ? linha : nova_linha;
        }

        private static string IdentaJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return "";
            string resultado = "";
            int nivel_ident = 0;

            for (int i = 0; i < json.Length; i++)
            {
                if (json[i] == ',')
                {
                    resultado += json[i] + "\n";
                    for (int j = 0; j < nivel_ident; ++j) resultado += "  ";
                    continue;
                }
                if (json[i] == '{' || json[i] == '[')
                {
                    nivel_ident++;
                    resultado += json[i] + "\n";
                    for (int j = 0; j < nivel_ident; ++j) resultado += "  ";
                    continue;
                }
                if (json[i] == '}' || json[i] == ']')
                {
                    nivel_ident--;
                    resultado += "\n";
                    for (int j = 0; j < nivel_ident; ++j) resultado += "  ";
                    resultado += json[i];
                    continue;
                }
                resultado += json[i];
            }
            return resultado;
        }
    }
}
