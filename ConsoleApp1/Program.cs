using System.Text;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Args argumentos = new(args);

            AbreArquivo(argumentos.Caminho, out string conteudo);
            if (string.IsNullOrEmpty(conteudo))
            {
                Console.WriteLine("Arquivo vazio. Saindo da aplicação...");
                return;
            }

            App app = ParseArquivo(conteudo);
            foreach (var rota in app.Rotas)
            {
                rota.FazRequisicao(app.Variaveis);
                if (rota.Resposta is null) throw new Exception("Erro. Rota não retornou resposta.");

                var corTexto = Console.ForegroundColor;
                if (rota.Resposta.Codigo < 200 || rota.Resposta.Codigo >= 300)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }

                Console.WriteLine($"({rota.Resposta.Codigo})");
                Console.ForegroundColor = corTexto;
                if (argumentos.MostrarResultado) Console.WriteLine(rota.Resposta.Body);
            }
        }

        private static App ParseArquivo(string conteudo)
        {
            var app = new App();

            List<string> linhas = conteudo.Split('\n').Select(l => l.Trim()).ToList();
            for (var i = 0; i < linhas.Count; ++i)
            {
                if (linhas[i] == "" || linhas[i] == "#" || linhas[i].StartsWith("//")) continue;

                // VARIABLES Parsing
                if (linhas[i][0] == '@')
                {
                    var v = linhas[i].Split('=');
                    var nome = v[0].Substring(1);
                    bool temPrompt = v[1].Trim()[0] == '?';
                    var valor = v[1].Trim();
                    app.Variaveis.Add(new Variavel
                    {
                        Nome = nome,
                        Valor = valor,
                    });
                    continue;
                }

                // TODO(Andre): Essa implementacao nao esta considerando o delimitador '###' de fim de rota.
                // Da mesma forma, não está considerando o corpo de resposta e seus delimitadores '{' e '}'.
                // Uma implementacao que tente desconsidera-los, como essa, pode se tornar mais complicada de implementar e entender.
                // TODO(Andre): Implementar o parsing do corpo da requisicao
                Rota rota = new Rota();
                if (linhas[i].StartsWith("GET") || linhas[i].StartsWith("POST") || linhas[i].StartsWith("PUT") || linhas[i].StartsWith("DELETE"))
                {
                    var linha_rota = linhas[i].Split(' ');
                    rota.Metodo = linha_rota[0];
                    rota.UrlCrua = linha_rota[1];
                    app.Rotas.Add(rota);

                    // HEADERS Parsing
                    // O delimitador final dos headers DEVE SER uma linha vazia.
                    while (true)
                    {
                        i++;
                        if (linhas[i] == "") break;
                        var header = linhas[i].Split(':');
                        if (header == null || header.Length < 2) throw new Exception($"Erro de sintaxe no header. Linha: {i}\nSeção dos Headers deve finalizar com uma linha em branco.");
                        string nome_header = header[0].Trim();
                        string valor_header = header[1].Trim();
                        int essaRota = app.Rotas.Count - 1;
                        app.Rotas[essaRota].Header.Add(nome_header, valor_header);
                    }

                    bool fimDoArquivo = false;
                    while (linhas[i] == "")
                    {
                        i++; // avança uma linha para verificar se tem body
                        if (i >= linhas.Count)
                        {
                            fimDoArquivo = true; // ultima rota e não contém json body
                            break;
                        }
                    }
                    if (fimDoArquivo) break;

                    // BODY Parsing (JSON)
                    if (linhas[i][0] == '{')
                    {
                        string body = "";
                        int nivelJson = 0;
                        while (true)
                        {
                            bool end = false;
                            for (int j = 0; j < linhas[i].Length; ++j)
                            {
                                body += linhas[i][j];
                                if (linhas[i][j] == '{') nivelJson++;
                                if (linhas[i][j] == '}') nivelJson--;
                                if (nivelJson == 0)
                                {
                                    end = true;
                                    break;
                                }
                            }
                            i++;
                            if (i >= linhas.Count) throw new Exception("Erro de sintaxe. Provavelmente erro no json body da rota " + rota.UrlCrua);
                            if (end) break;
                        }
                        rota.Body = body;
                    }
                    else
                    {
                        // Rota não tem body, desfaz avanço pra próxima linha
                        i--;
                    }
                    continue;
                }
            }
            return app;
        }

        private static void AbreArquivo(string caminho, out string conteudo)
        {
            conteudo = "";
            try
            {
                conteudo = File.ReadAllText(caminho);
            }
            catch (Exception e)
            {
                Console.WriteLine("Erro ao abrir arquivo '" + caminho + "'. " + e.Message);
            }
        }
    }

    class Args
    {
        public string Caminho { get; set; } = "";
        public bool MostrarResultado { get; set; } = false;

        public Args(string[] args)
        {
            if (args.Length == 0)
            {
                MostraUso();
                return;
            }
            Caminho = args[0];
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
                    return;
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
    public struct Variavel
    {
        public string Nome;
        public string Valor;
    }

    public class Rota
    {
        public string? Metodo { get; set; }
        public string? UrlCrua { get; set; }
        public string? Url { get; set; }
        public Dictionary<string, string> Header { get; set; } = new();
        public string? Body { get; set; }
        public Resposta? Resposta { get; set; }

        public void FazRequisicao(List<Variavel> vars)
        {
            if (UrlCrua is null) throw new Exception("Erro. URL da rota não definida.");
            Url = SubstituiVariaveis(UrlCrua, vars);
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(new HttpMethod(Metodo!), Url);
            foreach (var header in Header)
            {
                if (header.Key.ToLower() == "content-type") continue; // Content-Type é definido no Body
                string value = SubstituiVariaveis(header.Value, vars);
                request.Headers.Add(header.Key, value);
            }
            if (Body != null)
            {
                request.Content = new StringContent(Body, Encoding.UTF8, "application/json");
            }
            // TODO(Andre): não perguntar novamente se a variável for a mesma nas rotas seguintes
            int promptIndex = Url.IndexOf("=?"); // variavel de query
            if (promptIndex == -1) promptIndex = Url.IndexOf("/?"); // variavel de rota
            if (promptIndex >= 0)
            {
                int endPromptIndex = Url.IndexOf("?", promptIndex + 2);
                string prompt = Url.Substring(promptIndex + 2, endPromptIndex - promptIndex - 2);
                Console.Write(prompt + " ");
                string resposta = Console.ReadLine() ?? throw new Exception($"Erro. Valor precisa ser inserido.");
                string nova_rota = Url.Substring(0, promptIndex + 1) + resposta + Url.Substring(promptIndex + 2 + prompt.Length + 1);
                Url = nova_rota;
            }
            Console.Write($"\n[{Metodo}] {Url}... ");
            HttpResponseMessage response = client.SendAsync(request).Result;
            Resposta = new Resposta
            {
                Codigo = (int)response.StatusCode,
                Body = IdentaJson(response.Content.ReadAsStringAsync().Result)
            };
        }

        private string SubstituiVariaveis(string linha, List<Variavel> vars)
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
                        throw new Exception("Erro de sintaxe na linha: " + linha);
                    }
                    valor_var = vars.FirstOrDefault(v => v.Nome == nome_var).Valor
                        ?? throw new Exception($"Não foi possível obter o valor da variável {nome_var}. Verifique se ela foi declarada anteriormente.");

                    nova_linha += valor_var;
                    vars.Add(new Variavel { Nome = nome_var, Valor = valor_var });
                    i++;
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

    public class Resposta
    {
        public int? Codigo { get; set; }
        public string? Body { get; set; }
    }

    public class App
    {
        public List<Variavel> Variaveis { get; set; } = new();
        public List<Rota> Rotas { get; set; } = new();
    }
}
