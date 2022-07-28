namespace MousePos
{
    internal static class Program
    {
        private static Mutex mutex = null;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            const string appName = "OneButton";
            bool createdNew;
            mutex = new Mutex(true, appName, out createdNew);
            if (!createdNew)
            {
                //app is already running! Exiting the application
                return;
            }
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }
}