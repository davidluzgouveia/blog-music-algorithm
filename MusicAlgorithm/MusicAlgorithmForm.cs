namespace MusicAlgorithm
{
    using System;
    using System.CodeDom.Compiler;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Audio;

    public partial class MusicAlgorithmForm : Form
    {
        public MusicAlgorithmForm()
        {
            InitializeComponent();
            PopulateDemoList();
        }

        private void PopulateDemoList()
        {
            demoList.Items.Add(@"");
            demoList.Items.Add(@"(t>>6|t|t>>(t>>16))*10+((t>>11)&7)");
            demoList.Items.Add(@"(t*(4|7&t>>13)>>((~t>>11)&1)&128) + ((t)*(t>>11&t>>13)*((~t>>9)&3)&127)");
            demoList.Items.Add(@"((t*(t>>8|t>>9))&46&t>>8)^(t&t>>13|t>>6)");
            demoList.Items.Add(@"(t&t%255)-(t*3&t>>13&t>>6)");
            demoList.Items.Add(@"t*((t>>12|t>>8)&63&t>>4)");
            demoList.Items.Add(@"t*(t>>11&t>>8&123&t>>3)");
            demoList.Items.Add(@"t*((t>>9|t>>13)&25&t>>6) ");
            demoList.Items.Add(@"(t*(t>>5|t>>8))>>(t>>16)");
            demoList.Items.Add(@"t*5&(t>>7)|t*3&(t*4>>10)");
            demoList.Items.Add(@"(t|(t>>9|t>>7))*t&(t>>11|t>>9)");
            demoList.SelectedIndex = 0;
        }

        protected override void OnLoad(EventArgs e)
        {
            // Create byte buffer for storing and submitting audio samples
            _buffer = new byte[256*2];
 
            // Create a new DynamicSoundEffectInstance at 8Khz Mono
            _instance = new DynamicSoundEffectInstance(8000, AudioChannels.Mono);
            
            // Creates and starts the audio thread
            new Thread(AudioThread).Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            // Triggers the audio thread to close
            _running = false;
        }

        private void AudioThread()
        {
            // Service the XNA Framework once before everything is started
            FrameworkDispatcher.Update();

            // Start the DynamicSoundEffectInstance
            _instance.Play();

            // Go into a loop that repeats around 20 times per second (50ms sleep)
            while (_running)
            {
                // Service the XNA Framework once per loop
                FrameworkDispatcher.Update();

                // Fill and submit buffer
                while (_instance.PendingBufferCount < 3) 
                    SubmitBuffer();
                
                Thread.Sleep(50);
            }
        }

        private void SubmitBuffer()
        {
            // Fill the buffer and advance time
            for (int i = 0; i != _buffer.Length; ++i, ++_time)
                _buffer[i] = (byte) GetSample();

            // Submit buffer
            _instance.SubmitBuffer(_buffer);
        }

        private int GetSample()
        {
            // Redirects the call to our compiled code if any
            return _generatorType != null ? (int) _generatorType.GetMethod("Generate").Invoke(null, new object[] {_time}) : 0;
        }

        private void SetAlgorithm(string algorithm)
        {
            // Reset values
            _time = 0;
            _generatorType = null;

            // Ignore if the user wrote nothing
            if (String.IsNullOrWhiteSpace(algorithm))
                return;

            // Create code string
            StringBuilder codeBuilder = new StringBuilder();
            codeBuilder.AppendLine("namespace MusicAlgorithm {");
            codeBuilder.AppendLine("    public static class AudioGenerator {");
            codeBuilder.AppendLine("        public static int Generate(int t) { return " + algorithm + "; }");
            codeBuilder.AppendLine("    }");
            codeBuilder.AppendLine("}");
            string code = codeBuilder.ToString();

            // Compile code string in memory
            CodeDomProvider codeDomProvider = CodeDomProvider.CreateProvider("C#");
            CompilerParameters compileParams = new CompilerParameters { GenerateExecutable = false, GenerateInMemory = true };
            CompilerResults compilerResults = codeDomProvider.CompileAssemblyFromSource(compileParams, new[] {code});
            
            // On error display message and exit
            if (compilerResults.Errors.HasErrors)
            {
                MessageBox.Show(GetErrorMessage(compilerResults));
                return;
            }

            // Otherwise store a Type reference to the assembly we compiled
            // Using reflection we can call the Generate method from this Type
            _generatorType = compilerResults.CompiledAssembly.GetType("MusicAlgorithm.AudioGenerator");
        }

        private static string GetErrorMessage(CompilerResults compilerResults)
        {
            StringBuilder errorBuilder = new StringBuilder();
            foreach (CompilerError compilerError in compilerResults.Errors)
                errorBuilder.AppendLine(compilerError.ErrorText);
            return errorBuilder.ToString();
        }

        private void PlayButtonClick(object sender, EventArgs e)
        {
            SetAlgorithm(codeBar.Text);
        }

        private void StopButtonClick(object sender, EventArgs e)
        {
            SetAlgorithm("0");
        }

        private void DemoListSelectedIndexChanged(object sender, EventArgs e)
        {
            codeBar.Text = (string)demoList.SelectedItem;
        }

        private DynamicSoundEffectInstance _instance;
        private byte[] _buffer;
        private int _time;
        private Type _generatorType;
        private bool _running = true;
    }
}
