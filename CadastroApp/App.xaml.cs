namespace CadastroApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage())
            {
                Title = "CadastroApp",

                // Tamanho inicial usado em plataformas desktop
                Width = 1500,
                Height = 850,

                // Impede que a janela fique pequena demais
                MinimumWidth = 1100,
                MinimumHeight = 700
            };



            return window;
        }
    }
}