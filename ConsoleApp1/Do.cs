namespace RouteChecker
{
    public static class Do
    {
        public static void Kill(string mensagem)
        {
            Console.WriteLine(mensagem);
            Environment.Exit(1);
        }
    }
}
