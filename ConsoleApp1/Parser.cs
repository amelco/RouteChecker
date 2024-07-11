namespace RouteChecker
{
    public class Parser
    {
        private string conteudo = "";
        public Dictionary<string, string> Variaveis { get; set; } = new();

        public Parser(Args argumentos)
        {
            try
            {
                conteudo = File.ReadAllText(argumentos.CaminhoArquivo);
                if (string.IsNullOrEmpty(conteudo)) Do.Kill("Arquivo vazio. Saindo da aplicação...");
            }
            catch (Exception e)
            {
                Do.Kill("Erro ao abrir arquivo '" + argumentos.CaminhoArquivo + "'. " + e.Message);
            }
        }

        public List<Rota> Parse()
        {
            List<Rota> rotas = new();
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
                    Variaveis.Add(nome, valor);
                    continue;
                }

                Rota rota = new Rota();
                if (linhas[i].StartsWith("GET") || linhas[i].StartsWith("POST") || linhas[i].StartsWith("PUT") || linhas[i].StartsWith("DELETE"))
                {
                    var linha_rota = linhas[i].Split(' ');
                    rota.Metodo = linha_rota[0];
                    rota.UrlCrua = linha_rota[1];
                    rotas.Add(rota);

                    // HEADERS Parsing
                    // O delimitador final dos headers DEVE SER uma linha vazia.
                    while (true)
                    {
                        i++;
                        if (linhas[i] == "") break;
                        var header = linhas[i].Split(':');
                        if (header == null || header.Length < 2) Do.Kill($"Erro de sintaxe no header. Linha: {i}\nSeção dos Headers deve finalizar com uma linha em branco.");
                        string nome_header = header[0].Trim();
                        string valor_header = header[1].Trim();
                        int essaRota = rotas.Count - 1;
                        rotas[essaRota].Header.Add(nome_header, valor_header);
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
                            if (i >= linhas.Count) Do.Kill("Erro de sintaxe. Provavelmente erro no json body da rota " + rota.UrlCrua);
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
            return rotas;
        }
    }
}
