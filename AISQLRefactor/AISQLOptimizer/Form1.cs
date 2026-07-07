#pragma warning disable OPENAI001
using Azure;
using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Web.WebView2.Core;
using OpenAI.Responses;
using System.ClientModel;
using System.Data;
using System.Text;
using System.Threading;
using System.Windows.Forms;


namespace AISQLOptimizer
{
    public partial class Form1 : Form
    {
        private readonly AppState _state = new AppState();
        private AIProjectClient _projectClient;
        private ProjectResponsesClient _responseClient;
        private string _previousResponseId;            // sostituisce il thread Foundry
        private InteractiveBrowserCredential _credential;
        private int _idnode;
        private bool _runningRefactor = false;
        private TimeSpan _lastAgentElapsed;   // elaboration time of the last agent call
        private const int MaxAzureMessageChars = 768_000;
        private const int MaxSchemaChars = 768_000;

        private ContextMenuStrip _treeContextMenu;
        private TreeNode _rightClickedNode;

        private Panel panelTop;
        private string _focusToControl = "";
        private int _totalTokensInput = 0;
        private int _totalTokensOutput = 0;

        private readonly Dictionary<TreeNode, long> _treeNodeNumbers = new();
        private readonly HashSet<int> _selectedIds = new();
        private const int TREE_NUMBER_COLUMN_WIDTH = 60;
        private const int TREE_COLUMN_GAP = 8;
        private const int TREE_MIN_TEXT_WIDTH = 20; // minimum visibile
        private const int TREE_NUMBER_RIGHT_MARGIN = 24;

        private readonly Dictionary<Button, System.Windows.Forms.Timer> _blinkTimers = new Dictionary<Button, System.Windows.Forms.Timer>();

        //split1
        private Panel _splitter;
        private double _splitRatio = 0.46;
        private bool _draggingSplit = false;

        //split tree (treeView1 | richTextBox1)
        private Panel _splitterTree;
        private double _treeRatio = 0.18;
        private bool _draggingTree = false;

        //Opensave buttons
        private Label _lblSaveSession;
        private Label _lblOpenSession;
        private Label _lblImpact;           // pulsante dashboard 
        private FormImpact _formImpact;     // istanza unica della dashboard
        private Label _lblOriginalCode;
        private ToolTip _topBarTips;

        //gestione lost Thread
        private bool _threadContextLost = false;    // true if thread Azure was 404 e ne abbiamo creato uno nuovo
        private string _lostSourceSql = "";         // sourcesql da reiniettare al primo follow-up
        private string _lostOptimizedHtml = "";     // AI_optimized da reiniettare al primo follow-up

        //Costruttore
        public Form1()
        {
            InitializeComponent();
            InitializeTreeViewContextMenu();
            //double buffering attivato inline
            typeof(TreeView).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(treeView1, true);

            //Keeps visible the selected node even when the treeview does not have focus
            treeView1.HideSelection = false;

            this.FormClosing += Form1_FormClosing;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.Load += Form1_Load;

            treeView1.AfterCheck += treeView1_AfterCheck;
            treeView1.NodeMouseClick += treeView1_NodeMouseClick_ContextMenu;
            treeView1.NodeMouseClick += treeView1_NodeMouseClick;
            treeView1.LostFocus += treeView1_LostFocus;

            label7.MouseEnter += Label7_MouseEnter;
            label7.MouseLeave += Label7_MouseLeave;
            richTextBox2.KeyDown += richTextBox2_KeyDown;

            // Forces the label11 visibility to depend on Database Name
            label4.TextChanged += (s, e) => label11.Visible = !string.IsNullOrWhiteSpace(label4.Text);

            // Forces redesign on size change 
            treeView1.Resize += (s, e) => { treeView1.Refresh(); };
        }


        private async void Form1_Load(object sender, EventArgs e)
        {
            // --- Window setup
            this.StartPosition = FormStartPosition.CenterScreen;
            Rectangle screenArea = Screen.PrimaryScreen.WorkingArea;
            this.Size = new Size((int)(screenArea.Width * 0.92), (int)(screenArea.Height * 0.82));
            this.MinimizeBox = true;
            this.MaximizeBox = true;
            this.CenterToScreen();
            int WW = this.ClientSize.Width;
            int YY = this.ClientSize.Height;
            int TT = (int)(YY * 0.092);
            int LOWB = (int)(comboMetric.Height * 2.7);
            this.MinimumSize = new Size((int)(WW * 0.75), (int)(YY * 0.4));

            // ---- PANEL TOP 
            panelTop = new Panel();
            panelTop.Height = (int)(YY * 0.090); //80;
            panelTop.Dock = DockStyle.Top;
            panelTop.BackColor = Color.FromArgb(20, 90, 175);
            this.Controls.Add(panelTop);

            panelTop.Controls.Add(label1);
            panelTop.Controls.Add(label2);
            panelTop.Controls.Add(label3);
            panelTop.Controls.Add(label4);
            panelTop.Controls.Add(label5);
            panelTop.Controls.Add(label6);

            label8.Location = new Point((int)(WW * 0.25), (int)(TT * 0.008));
            label9.Location = new Point((int)(WW * 0.25), (int)(TT * 0.23));
            label5.Location = new Point((int)(WW * 0.25), (int)(TT * 0.488));
            label6.Location = new Point((int)(WW * 0.25), (int)(TT * 0.71));

            label8.BackColor = Color.Transparent;
            label8.Parent = panelTop;

            label9.BackColor = Color.Transparent;
            label9.Parent = panelTop;
            label10.Parent = panelTop;
            label10.BackColor = Color.Transparent;
            label10.ForeColor = Color.White;

            label10.AutoSize = true;
            label10.Font = new Font(label10.Font.FontFamily, (float)(YY * 0.030), FontStyle.Regular, GraphicsUnit.Pixel);

            // label12 
            label12.Parent = panelTop;
            label12.BackColor = Color.FromArgb(20, 90, 175);   
            label12.ForeColor = Color.White;
            label12.Font = new Font(label12.Font.FontFamily, (float)(YY * 0.0145), FontStyle.Regular, GraphicsUnit.Pixel);
            label12.Top = label10.Bottom + 2;   

            // label10 
            label10.BringToFront();

            label7.AutoSize = true;
            label7.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            label7.Font = new Font("Segoe MDL2 Assets", 34f);
            label7.Text = "\uE713";
            label7.Top = (panelTop.Height - label7.Height) / 2;
            label7.Left = panelTop.Width - label7.Width - 30;
            label7.BackColor = panelTop.BackColor;
            label7.Parent = panelTop;
            label11.Visible = false;
            InitializeSessionIcons();

            // ----Central layout
            int top = panelTop.Bottom;
            int spacing = 2;

            comboMetric.Top = top;
            comboMetric.Left = 6;
            comboMetric.Width = (int)(WW * 0.18);
            comboMetric.DisplayMember = "Key";
            comboMetric.ValueMember = "Value";

            comboMetric.Items.AddRange(new object[]
            {
                new KeyValuePair<string, string>("number of executions",  "executions"),
                new KeyValuePair<string, string>("cpu consumption (sec)", "cpu"),
                new KeyValuePair<string, string>("elapsed time (sec)",    "elapsed"),
                new KeyValuePair<string, string>("I/O reads",             "reads")
            });
            comboMetric.SelectedIndex = 0;
            comboMetric.SelectedIndexChanged += Combo_SelectionChanged;

            treeView1.Location = new Point(6, (int)(top + comboMetric.Height));
            treeView1.Width = (int)(WW * 0.18);
            treeView1.Height = YY - top - LOWB - 8;
            treeView1.BackColor = SystemColors.InactiveBorder;

            richTextBox1.Top = top;
            richTextBox1.Left = treeView1.Right + spacing;
            richTextBox1.BorderStyle = BorderStyle.None;
            richTextBox1.Width = (int)(WW * 0.38);
            richTextBox1.Height = YY - top - LOWB + comboMetric.Height - 8;

            webView21.Top = top;
            webView21.Left = richTextBox1.Right + spacing;
            webView21.Width = (int)(WW * 0.44) - 12;
            webView21.Height = YY - top - LOWB + comboMetric.Height - 8;


            //split
            _splitter = new Panel { Width = 6, Cursor = Cursors.VSplit, BackColor = Color.Gray };
            this.Controls.Add(_splitter);
            _splitter.BringToFront();
            _splitter.MouseDown += (s, e) => _draggingSplit = true;
            _splitter.MouseUp += (s, e) => _draggingSplit = false;
            _splitter.MouseMove += Splitter_MouseMove;

            //split tree
            _splitterTree = new Panel { Width = 6, Cursor = Cursors.VSplit, BackColor = Color.Gray };
            this.Controls.Add(_splitterTree);
            _splitterTree.BringToFront();
            _splitterTree.MouseDown += (s, e) => _draggingTree = true;
            _splitterTree.MouseUp += (s, e) => _draggingTree = false;
            _splitterTree.MouseMove += SplitterTree_MouseMove;


            label1.Location = new Point(webView21.Left, (int)(TT * 0.008));
            label2.Location = new Point(webView21.Left, (int)(TT * 0.23));
            label11.Location = new Point(webView21.Left, (int)(TT * 0.50));
            label3.Location = new Point(webView21.Left + (int)(WW * 0.05), (int)(TT * 0.008));
            label4.Location = new Point(webView21.Left + (int)(WW * 0.05), (int)(TT * 0.23));
            label11.Parent = panelTop;
            label11.BackColor = Color.LightGreen;

            button3.Location = new Point((int)(richTextBox1.Left), (int)(treeView1.Bottom + TT * 0.1));
            button3.Size = new Size((int)(TT * 3), (int)(comboMetric.Height * 1.25));

            button4.Location = new Point(5, (int)(treeView1.Bottom + TT * 0.1));
            button4.Size = new Size((int)(TT * 3), (int)(comboMetric.Height * 1.25));

            richTextBox2.Size = new Size((int)(WW * 0.34), (int)(comboMetric.Height * 1.25));
            richTextBox2.Location = new Point((int)(WW * 0.58), (int)(treeView1.Bottom + TT * 0.1));
            richTextBox2.BorderStyle = BorderStyle.None;
            richTextBox2.Font = new Font(richTextBox2.Font.FontFamily, 11);

            button2.Location = new Point((int)(richTextBox2.Right + 10), (int)(treeView1.Bottom + TT * 0.1));
            button2.Size = new Size((int)(TT), (int)(comboMetric.Height * 1.25));

            treeView1.DrawMode = TreeViewDrawMode.OwnerDrawText;
            treeView1.DrawNode += treeView1_DrawNode;

            // ---------Dynamic resize of the controls 
            this.Resize += (s, ev) =>
            {
                WW = this.ClientSize.Width;

                treeView1.Width = (int)(WW * 0.18);
                treeView1.Height = ClientSize.Height - top - LOWB;

                richTextBox1.Height = ClientSize.Height - top - LOWB + comboMetric.Height;
                richTextBox1.Left = treeView1.Right + spacing;
                richTextBox1.Width = (int)(WW * 0.38);

                webView21.Left = richTextBox1.Right + spacing;
                webView21.Width = (int)(WW * 0.44) - 12;
                webView21.Height = ClientSize.Height - top - LOWB + comboMetric.Height;

                label1.Location = new Point(webView21.Left, (int)(TT * 0.008));
                label2.Location = new Point(webView21.Left, (int)(TT * 0.23));
                label11.Location = new Point(webView21.Left, (int)(TT * 0.50));
                label3.Location = new Point(webView21.Left + (int)(WW * 0.05), (int)(TT * 0.008));
                label4.Location = new Point(webView21.Left + (int)(WW * 0.05), (int)(TT * 0.23));

                button2.Top = top + webView21.Height + 10;
                comboMetric.Width = (int)(WW * 0.18);

                richTextBox2.Width = (int)(WW * 0.34);
                richTextBox2.Location = new Point((int)(WW * 0.58), top + webView21.Height + 10);
                button2.Left = richTextBox2.Right + 10;
                button3.Location = new Point((int)(richTextBox1.Left), (int)(treeView1.Bottom + LOWB * 0.1));
                button4.Location = new Point(5, (int)(treeView1.Bottom + LOWB * 0.1));

                label5.Left = (int)(WW * 0.25);
                label6.Left = (int)(WW * 0.25);
                label8.Left = (int)(WW * 0.25);
                label9.Left = (int)(WW * 0.25);
                LayoutTreeArea();
            };
            // label "Original code" over richTextBox1  
            _lblOriginalCode = new Label
            {
                Text = "Original code",
                AutoSize = true,
                ForeColor = Color.Gray,
                BackColor = richTextBox1.BackColor,
                Font = new Font("Arial", 12f),
                Padding = new Padding(2, 1, 2, 1)
            };
            this.Controls.Add(_lblOriginalCode);
            _lblOriginalCode.BringToFront();

            LayoutTreeArea();
            await InitializeWebView2Async();
        }

        private void Label7_MouseEnter(object sender, EventArgs e)
        {
            label7.ForeColor = Color.LightGray;   // hover
        }


        private void Label7_MouseLeave(object sender, EventArgs e)
        {
            UpdateSettingsIndicator();
        }


        private void UpdateSettingsIndicator()
        {
            bool agentSelected = _responseClient != null;
            bool databaseSelected = !string.IsNullOrWhiteSpace(_state.SqlDatabase);

            label7.ForeColor = (agentSelected && databaseSelected)
                ? Color.LightGreen
                : Color.White;
        }


        private void treeView1_LostFocus(object sender, EventArgs e)
        {
            // Il BeginInvoke allows to read ActiveControl after the focus has shifted
            this.BeginInvoke(new Action(() => { _focusToControl = this.ActiveControl.Name; }));
        }


        private void StartBlinking(Button btn)
        {
            if (btn == null)
                return;

            if (!_blinkTimers.TryGetValue(btn, out var timer))
            {
                timer = new System.Windows.Forms.Timer();
                timer.Interval = 1600;
                timer.Tick += (s, e) => { btn.ForeColor = btn.ForeColor == Color.LightGreen ? Color.Green : Color.LightGreen; };
                _blinkTimers[btn] = timer;
            }
            btn.ForeColor = Color.LightGreen;
            timer.Start();
        }


        private void StopBlinking(Button btn)
        {
            if (btn == null) return;

            if (_blinkTimers.TryGetValue(btn, out var timer))
            {
                timer.Stop();
            }
            btn.ForeColor = Color.White;
        }


        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var timer in _blinkTimers.Values.ToList())
            {
                timer.Stop();
                timer.Dispose();
            }
            _blinkTimers.Clear();
        }


        private void richTextBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;  
                button2.PerformClick();     
            }
        }

        private async Task InitializeWebView2Async()
        {
            Color inactiveBorder = SystemColors.InactiveBorder;
            string coloreHtml = $"rgb({inactiveBorder.R}, {inactiveBorder.G}, {inactiveBorder.B})";

            // Make sure WebView2 initialized
            await webView21.EnsureCoreWebView2Async(null);
            string htmlReset = $@"
                    <html>
                    <head>
                        <style>
                            body {{
                                font-family: Arial;
                                margin: 0;
                                padding: 8px;
                                height: 100%;
                                overflow-y: scroll;
                                font-size: 14px;
                                background-color: {coloreHtml};
                            }}
                            #content {{
                                display: flex;
                                flex-direction: column;
                            }}
                        </style>
                    </head>
                    <body>
                        <div style='position:fixed; top:0; right:4px; color:gray; background-color:{coloreHtml}; font-family:Arial; font-size:16px; padding:2px 4px; z-index:1000;'>Optimized code</div>
                        <div id='content' style='margin-top:850px; font-size: 14px;'>
                            <div style='font-size:24px'>Welcome to SQL AI Refactoring tool!</div>
                        </div>
                        <script>
                            document.body.style.zoom = '70%';
                        </script>
                    </body>
                    </html>";

            // Loads the initial HTML content directly
            webView21.CoreWebView2.NavigateToString(htmlReset);
        }


        private void ResetWebView2()
        {
            // If WebView2 is not initialized, do nothing (the caller should have awaited initialization)
            if (webView21.CoreWebView2 == null)
                return;

            Color inactiveBorder = SystemColors.InactiveBorder;
            string coloreHtml = $"rgb({inactiveBorder.R}, {inactiveBorder.G}, {inactiveBorder.B})";

            string htmlReset = $@"
                            <html>
                            <head>
                                <style>
                                    body {{
                                        font-family: Arial;
                                        margin: 0;
                                        padding: 8px;
                                        height: 100%;
                                        overflow-y: scroll;
                                        font-size: 14px;
                                        background-color: {coloreHtml};
                                    }}
                                    #content {{
                                        display: flex;
                                        flex-direction: column;
                                    }}
                                </style>
                            </head>
                            <body>
                                <div style='position:fixed; top:0; right:4px; color:gray; background-color:{coloreHtml}; font-family:Arial; font-size:16px; padding:2px 4px; z-index:1000;'>Optimized code</div>
                                <div id='content' style='margin-top:850px; font-size: 14px;'>
                                </div>
                                <script>
                                    document.body.style.zoom = '70%';
                                    document.body.style.backgroundColor = '{coloreHtml}';
                                </script>
                            </body>
                            </html>";

            // Reset 
            webView21.CoreWebView2.Navigate("about:blank");
            webView21.CoreWebView2.NavigateToString(htmlReset);
        }


        private void AggiungiContenutoHtmlScroll(string nuovoHtml)
        {
            Color inactiveBorder = SystemColors.InactiveBorder;
            string coloreHtml = $"rgb({inactiveBorder.R}, {inactiveBorder.G}, {inactiveBorder.B})";

            string script = $@"
            document.body.style.backgroundColor = '{coloreHtml}';
            var contentDiv = document.getElementById('content');
            var nuovoDiv = document.createElement('div');
            nuovoDiv.innerHTML = `" + nuovoHtml.Replace("`", "\\`") + @"`;
            contentDiv.appendChild(nuovoDiv);
            document.body.style.zoom = '70%';

            function smoothScrollToBottom(duration = 2000) {
                const start = window.scrollY;
                const end = document.body.scrollHeight;
                const distance = end - start;
                const startTime = performance.now();

                function scrollStep(currentTime) {
                    const elapsed = currentTime - startTime;
                    const progress = Math.min(elapsed / duration, 1);
                    window.scrollTo(0, start + distance * easeInOutQuad(progress));
                    if (progress < 1) {
                        requestAnimationFrame(scrollStep);
                    }
                }
                function easeInOutQuad(t) {
                    return t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;
                }
                requestAnimationFrame(scrollStep);
            }

            smoothScrollToBottom(2000); // ← cambia qui la durata in ms per più lentezza";
            webView21.CoreWebView2.ExecuteScriptAsync(script);
        }


        private void AggiungiContenutoHtmlSenzaScroll(string testoPuro) //manina
        {
            string htmlFormattato = $@"
            <div style='margin: 10px 0; font-family: Arial; font-size: 18px; color: #007bff;'>
                {System.Net.WebUtility.HtmlEncode(testoPuro)}
            </div>";

            string script = @"
                    var contentDiv = document.getElementById('content');
                    if (!contentDiv) {
                        contentDiv = document.createElement('div');
                        contentDiv.id = 'content';
                        document.body.appendChild(contentDiv);
                    }

                    var nuovoDiv = document.createElement('div');
                    nuovoDiv.innerHTML = `" + htmlFormattato.Replace("`", "\\`") + @"`;
                    contentDiv.appendChild(nuovoDiv);
                    window.scrollTo(0, document.body.scrollHeight);";

            webView21.CoreWebView2.ExecuteScriptAsync(script);
        }


        ///Check if Agent is correctly configured
        private bool EnsureAgentReady()
        {
            if (_responseClient == null || string.IsNullOrWhiteSpace(_state.SelectedAgentName))
            {
                MessageBox.Show(
                    "Azure AI Foundry is not connected.\nOpen Settings and select an agent.",
                    "Not connected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }


        ////Optimize WINDOW contained code
        private async void button3_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(richTextBox1.Text))
            {
                MessageBox.Show("Specify the code you want to optimize");
                return;
            }

            //Check Agent configured
            if (!EnsureAgentReady()) return;

            if (_runningRefactor)
            {
                MessageBox.Show("Code optimization is running. Wait for its completion", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Too much code for the model (768k char limit in 'content')
            if (richTextBox1.Text.Length > MaxAzureMessageChars)
            {
                MessageBox.Show(
                    $"The code is too large for the model: {richTextBox1.Text.Length:N0} characters (limit {MaxAzureMessageChars:N0}).\nReduce it or split it into smaller parts.",
                    "Code too large",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_state.ConnectionString))
            {
                MessageBox.Show("SQL Server is not connected. The optimization will be performed without schema or index information and will be based on SQL syntax only.", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            //Graphics start button3
            button3.Text = "⏳ Optimizing...";
            button3.Font = new Font(button3.Font, FontStyle.Bold);
            button3.ForeColor = Color.LightGreen;
            StartBlinking(button3);
            _runningRefactor = true;

            try
            {
                //If the code in the window is related to a tree object, set the node bold and save the id for later update with AI optimized code
                int idNode = _idnode = 0;
                TreeNode selectedNode = treeView1.SelectedNode;

                if (_focusToControl == "button3" && treeView1.SelectedNode != null && treeView1.SelectedNode.Nodes.Count == 0)
                {
                    //the Window content recognized as tree object
                    idNode = (int)selectedNode.Tag;
                    _idnode = idNode;
                    selectedNode.NodeFont = new Font(treeView1.Font.FontFamily, treeView1.Font.Size + 0.5f, FontStyle.Bold);
                    treeView1.Refresh();
                }

                string codeToOptimize = richTextBox1.Text;
                AggiungiContenutoHtmlSenzaScroll("👉 Code from window sent to Agent");
                ResetAgentContext();

                //call the Foundry Agent!
                string risposta = await RunAgentConversation("Analyze, optimize and rewrite this T-SQL code: " + codeToOptimize);

                // Estrae i 4 punteggi dal marcatore <!--IMPACT ...--> e lo rimuove dal testo.
                risposta = ImpactExtractor.ExtractAndStrip(risposta, out var impact);

                //Refine content from Agent, show and save
                string htmlClean = risposta.Replace("```html", "").Replace("```", "").Trim();
                htmlClean = htmlClean + $"[Model:{_state.SelectedAgentModel}] time: {FormatElapsed(_lastAgentElapsed)}";
                htmlClean = "<div>" + htmlClean + "</div>";
                AggiungiContenutoHtmlScroll(htmlClean);

                // Save optimized code in memory if the code in the window is related to a tree object (idNode>0), for later update in the tree and database with the "Save" button
                if (idNode > 0)
                {
                    var row = _state.CodeplexRows.FirstOrDefault(r => r.Id == idNode);
                    if (row != null)
                    {
                        row.AiOptimized = htmlClean;
                        row.ThreadId = _previousResponseId;
                        ImpactExtractor.Apply(row, impact);
                    }
                }
            }
            catch (Exception ex)
            {
                AggiungiContenutoHtmlSenzaScroll($"💥 Optimization failed: {ex.Message}");
                AIUtility.TraceLog("button3_Click: " + ex);
            }
            finally
            {
                button3.Text = "Optimize window code";
                button3.Font = new Font(button3.Font, FontStyle.Regular);
                button3.ForeColor = Color.White;
                StopBlinking(button3);
                _runningRefactor = false;
            }
        }


        //Contact Agent and ask for work (Azure AI Foundry Agent Service - Responses API)
        private async Task<string> RunAgentConversation(string userMessage, bool sendSchema = true)
        {
            CreateResponseOptions Build(bool fresh)
            {
                var o = new CreateResponseOptions();

                if (fresh)
                {
                    if (_threadContextLost)
                    {
                        if (!string.IsNullOrWhiteSpace(_lostSourceSql))
                            o.InputItems.Add(ResponseItem.CreateUserMessageItem("For context - the original T-SQL that was optimized earlier:\n" + _lostSourceSql));
                        if (!string.IsNullOrWhiteSpace(_lostOptimizedHtml))
                            o.InputItems.Add(ResponseItem.CreateUserMessageItem("For context - the optimization previously proposed for it:\n" + _lostOptimizedHtml));
                    }

                    if (!string.IsNullOrEmpty(_state.SqlServerVersion))
                    {
                        string versionLine = _state.SqlServerVersion == "SQLAZURE"
                            ? "The target is Azure SQL (Database or Managed Instance): an evergreen engine that always runs the latest SQL Database Engine. You may use current T-SQL features."
                            : $"The target is SQL Server {_state.SqlServerVersion}. Use ONLY T-SQL syntax and features supported by this version. ";
                        string msgVersion = "=== TARGET SQL SERVER VERSION ===\n" + versionLine;
                        o.InputItems.Add(ResponseItem.CreateUserMessageItem(msgVersion));
                    }

                    bool hasSchema = !string.IsNullOrWhiteSpace(_state.strColumnTypesList)
                                                      || !string.IsNullOrWhiteSpace(_state.strIndexesList);

                    if (!hasSchema)
                    {
                        AggiungiContenutoHtmlSenzaScroll(
                            "⚠️ No database schema available (not scanned, or insufficient permissions). Optimization based on SQL syntax only.");
                    }
                    else
                    {
                        string msgTables = $"=== DATABASE TABLES (COLUMNS + DATA TYPES) ===\n {_state.strColumnTypesList}";
                        string msgIndexes = $"=== DATABASE INDEXES ===\n{_state.strIndexesList}";

                        if (msgTables.Length + msgIndexes.Length > MaxSchemaChars)
                        {
                            AggiungiContenutoHtmlSenzaScroll(
                                $"⚠️ Schema too large for the model ({(msgTables.Length + msgIndexes.Length):N0} chars, limit {MaxSchemaChars:N0}). Optimization based on SQL syntax only.");
                        }
                        else
                        {
                            o.InputItems.Add(ResponseItem.CreateUserMessageItem(msgTables));
                            o.InputItems.Add(ResponseItem.CreateUserMessageItem(msgIndexes));
                        }
                    }
                }
                else
                {
                    o.PreviousResponseId = _previousResponseId;
                }

                o.InputItems.Add(ResponseItem.CreateUserMessageItem(userMessage));
                return o;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            ResponseResult response;
            try
            {
                try
                {
                    CreateResponseOptions options = Build(fresh: sendSchema || _previousResponseId == null);
                    response = await _responseClient.CreateResponseAsync(options);
                }
                catch (ClientResultException ex) when (ex.Status == 404)
                {
                    _previousResponseId = null;
                    _threadContextLost = !string.IsNullOrWhiteSpace(_lostSourceSql) || !string.IsNullOrWhiteSpace(_lostOptimizedHtml);
                    CreateResponseOptions options = Build(fresh: true);
                    response = await _responseClient.CreateResponseAsync(options);
                }
            }
            catch (ClientResultException ex) when (ex.Status == 400)
            {
                throw new Exception(FriendlyApiError(ex), ex);
            }
            catch (ClientResultException ex) when (ex.Status == 429)
            {
                throw new Exception("Rate limit reached (too many requests or tokens per minute). Wait a few seconds and retry, or reduce the batch size.", ex);
            }
            finally
            {
                sw.Stop();
                _lastAgentElapsed = sw.Elapsed;
            }

            _threadContextLost = false;
            _lostSourceSql = "";
            _lostOptimizedHtml = "";

            _previousResponseId = response.Id;

            string lastResponse = response.GetOutputText() ?? string.Empty;
            if (response.Usage != null)
            {
                _totalTokensInput += response.Usage.InputTokenCount;
                _totalTokensOutput += response.Usage.OutputTokenCount;
            }

            label5.Text = "Input Tokens    : " + _totalTokensInput.ToString();
            label6.Text = "Output Tokens : " + _totalTokensOutput.ToString();

            return lastResponse;
        }


        // Formats an elapsed time as  M' SS''  (e.g. 1' 21'').
        private static string FormatElapsed(TimeSpan ts)
        {
            int minutes = (int)ts.TotalMinutes;
            int seconds = ts.Seconds;
            return $"{minutes}' {seconds:D2}''";
        }


        // Translates known Foundry/model 400 errors into a clear, actionable message; unknown 400s keep the raw text.
        private static string FriendlyApiError(ClientResultException ex)
        {
            string m = ex.Message ?? "";

            bool contextExceeded =
                   m.Contains("context_length", StringComparison.OrdinalIgnoreCase)
                || m.Contains("maximum context", StringComparison.OrdinalIgnoreCase)
                || m.Contains("string_above_max_length", StringComparison.OrdinalIgnoreCase)
                || (m.Contains("context", StringComparison.OrdinalIgnoreCase) && m.Contains("length", StringComparison.OrdinalIgnoreCase));
            if (contextExceeded)
                return "The request exceeds the model's context window: the code, the database schema, and the agent's instructions/knowledge are too large together. Reduce the selection or the code, or assign a model with a larger context window to the agent.";

            bool contentTooLarge =
                   m.Contains("content_size_exceeded", StringComparison.OrdinalIgnoreCase)
                || m.Contains("content size", StringComparison.OrdinalIgnoreCase)
                || m.Contains("too large", StringComparison.OrdinalIgnoreCase);
            if (contentTooLarge)
                return "The message content is too large for the service. Reduce the code or split it into smaller parts.";

            return "The model rejected the request (HTTP 400). Details: " + m;
        }


        ///Optimize OBJECT LIST
        private async void button4_Click(object sender, EventArgs e)
        {
            //check Agent configured
            if (!EnsureAgentReady()) return;

            // Verify that there's at least a selected node
            var selectedNodes = new List<TreeNode>();
            CollectCheckedNodes(treeView1.Nodes);
            if (selectedNodes.Count == 0)
            {
                MessageBox.Show("Select at least one object in the treeview to send to the Agent.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_runningRefactor)
            {
                MessageBox.Show("Code optimization is running. Wait for its completion", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Recursive local function to get all checked nodes.
            void CollectCheckedNodes(TreeNodeCollection nodes)
            {
                foreach (TreeNode node in nodes)
                {
                    if (node.Checked && node.Nodes.Count == 0) // solo foglie
                        selectedNodes.Add(node);
                    if (node.Nodes.Count > 0)
                        CollectCheckedNodes(node.Nodes);
                }
            }

            // Warns (only once) if the DB connection is missing
            if (string.IsNullOrWhiteSpace(_state.ConnectionString))
            {
                MessageBox.Show("SQL Server is not connected. The optimization will be performed without schema or index information and will be based on SQL syntax only.", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            //Start graphics
            button4.Text = "⏳ Optimizing...";
            button4.Font = new Font(button4.Font, FontStyle.Bold);
            button4.ForeColor = Color.LightGreen;
            StartBlinking(button4);
            _runningRefactor = true;

            //Select child nodes with active checkbox
            foreach (TreeNode parent in treeView1.Nodes)
            {
                foreach (TreeNode child in parent.Nodes)
                {
                    if (child.Checked)
                        child.NodeFont = new Font(treeView1.Font, FontStyle.Regular);
                }
            }
            treeView1.Refresh();
            AggiungiContenutoHtmlSenzaScroll($"👉 Sending {selectedNodes.Count} SQL objects to Code Optimizer agent...");

            // Main Loop
            int idNode = _idnode = 0;
            int totalNodes = selectedNodes.Count;
            int processedCount = 0;
            foreach (var selectedNode in selectedNodes)
            {
                ResetAgentContext();
                string fullName = selectedNode.Text.Trim();
                string sqlSource = string.Empty;
                idNode = (int)selectedNode.Tag;
                processedCount++;
                button4.Text = "⏳ Optimizing " + processedCount.ToString() + " of " + totalNodes.ToString();

                try
                {
                    // Retrieve the SQL source code for the selected object from the in-memory data.
                    sqlSource = _state.CodeplexRows.FirstOrDefault(r => r.Id == idNode)?.SourceSql;

                    if (string.IsNullOrWhiteSpace(sqlSource))
                    {
                        AggiungiContenutoHtmlSenzaScroll($"No SQL source found for object");
                        continue;
                    }
                }
                catch (Exception ex)
                {
                    AggiungiContenutoHtmlSenzaScroll($"Error retrieving data: {ex.Message}");
                    continue;
                }

                // Too much code for the model (768k char limit in 'content')
                if (sqlSource.Length > MaxAzureMessageChars)
                {
                    AggiungiContenutoHtmlSenzaScroll(
                        $"⏭️ Skipped {fullName}: source too large for the model ({sqlSource.Length:N0} chars, limit {MaxAzureMessageChars:N0}). Optimize it manually or split it.");
                    selectedNode.Checked = false;
                    treeView1.Refresh();
                    continue;
                }

                AggiungiContenutoHtmlSenzaScroll($"🔹 Optimizing object: {fullName}...");

                try
                {
                    //Set tree node bold
                    selectedNode.NodeFont = new Font(treeView1.Font.FontFamily, treeView1.Font.Size + 0.5f, FontStyle.Bold);
                    selectedNode.Text = selectedNode.Text + " ";
                    treeView1.SelectedNode = selectedNode;
                    treeView1.Refresh();

                    // Call AI Foundry Agent!
                    string response = await RunAgentConversation("Analyze, optimize and rewrite this T-SQL code: " + sqlSource);

                    //Extracts 4 scores <!--IMPACT ...--> and removes it from the text
                    response = ImpactExtractor.ExtractAndStrip(response, out var impact);

                    // Formatting the output for html display
                    string htmlClean = response.Replace("```html", "").Replace("```", "").Trim();
                    htmlClean = htmlClean + $"[Model:{_state.SelectedAgentModel}] time: {FormatElapsed(_lastAgentElapsed)}";
                    htmlClean = "<div>" + htmlClean + "</div>";

                    // Save optimized code in memory
                    var row = _state.CodeplexRows.FirstOrDefault(r => r.Id == idNode); 
                    if (row != null)
                    {
                        row.AiOptimized = htmlClean;
                        row.ThreadId = _previousResponseId;
                        ImpactExtractor.Apply(row, impact);

                        // Bold on current tree node even if the metric has changed.
                        TreeNode currentNode = FindNodeByTag(treeView1.Nodes, idNode);
                        if (currentNode != null)
                            currentNode.NodeFont = new Font(treeView1.Font.FontFamily, treeView1.Font.Size + 0.5f, FontStyle.Bold);
                    }
                }

                catch (Exception ex)
                {
                    AggiungiContenutoHtmlSenzaScroll($"💥 Error processing {fullName}: {ex.Message}");
                }

                //Pause to avoid rate limit
                await Task.Delay(400);

                //Uncheck checkbox in treeView 
                _selectedIds.Remove(idNode);
                TreeNode nodeToUncheck = FindNodeByTag(treeView1.Nodes, idNode);
                if (nodeToUncheck != null)
                {
                    treeView1.AfterCheck -= treeView1_AfterCheck;   
                    nodeToUncheck.Checked = false;
                    treeView1.AfterCheck += treeView1_AfterCheck;
                }
                treeView1.Refresh();
            }
            AggiungiContenutoHtmlSenzaScroll("✅ Optimization completed for all selected objects!");

            //Graphics effects
            button4.Text = "Optimize selected objects";
            button4.Font = new Font(button4.Font, FontStyle.Regular);
            button4.ForeColor = Color.White;
            StopBlinking(button4);
            _runningRefactor = false;
        }


        //Button GO!
        private async void button2_Click_1(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(richTextBox2.Text))
            {
                MessageBox.Show("Make a question.");
                return;
            }

            //Check Agent configured
            if (!EnsureAgentReady()) return;

            //no chat while processing is running: no follow up for GO button
            if (_runningRefactor)
            {
                MessageBox.Show("Code optimization is running. Wait for its completion.", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AggiungiContenutoHtmlSenzaScroll("👉 " + richTextBox2.Text);
            string userQuestion = richTextBox2.Text;
            richTextBox2.Text = "";
            button2.Text = "⏳";
            StartBlinking(button2);

            _runningRefactor = true;
            try
            {
                // Schema must be resent if the thread is new/fresh because the agent lost the previous context.
                bool freshThread = false;

                //Check presence of SQL in WebView2
                string sql = await ExtractSqlFromWebView2Async();
                if (sql.Length == 0)
                {
                    ResetAgentContext();
                    freshThread = true;
                }

                //Lost context server side. Let's force a fresh thread. Injection of context is done by RunAgentConversation.
                if (_threadContextLost)
                {
                    freshThread = true;
                }

                // GO! always uses the current code shown in the panel.
                // Send it as an explicit baseline so the agent edits the latest version, regardless of server-side context.
                string messageToAgent;
                if (sql.Length > 0)
                {
                    // Disable original reinjection: on 404 it would be sent first, making the agent fall back to the initial version.
                    _lostSourceSql = "";
                    _lostOptimizedHtml = "";
                    _threadContextLost = false;

                    messageToAgent =
                        "Work ONLY on the following T-SQL. It is the LATEST version; ignore any earlier or original version:\n\n"
                        + sql
                        + "\n\nApply the following change to the T-SQL above and return the complete updated T-SQL:\n"
                        + userQuestion;
                }
                else
                {
                    messageToAgent = userQuestion;
                }


                //Call the Agent! Schema sent only if the thread is new/fresh (first question or context lost), not on follow-up questions (GO!) because the context is already there.
                string risposta = await RunAgentConversation(messageToAgent, sendSchema: freshThread || _previousResponseId == null);

                //Extracts 4 scores <!--IMPACT ...--> and removes it from the text.
                risposta = ImpactExtractor.ExtractAndStrip(risposta, out var impact);

                //Format response from Agent
                risposta = risposta.Replace("```html", "").Replace("```", "");
                string nuovoHtml = "<div>" + risposta + "</div>";

                //string nuovoHtml 
                AggiungiContenutoHtmlScroll(nuovoHtml);

                //HTML in WebView2
                string rawHtmlJson = await webView21.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML;");
                string htmlcontent = System.Text.Json.JsonSerializer.Deserialize<string>(rawHtmlJson);

                // If node active, update row in memory
                if (_idnode > 0)
                {
                    var row = _state.CodeplexRows.FirstOrDefault(r => r.Id == _idnode);
                    if (row != null)
                    {
                        row.AiOptimized = htmlcontent;
                        ImpactExtractor.Apply(row, impact);
                    }
                }
            }
            catch (Exception ex)
            {
                AggiungiContenutoHtmlSenzaScroll($"💥 Operation failed: {ex.Message}");
                AIUtility.TraceLog("button2_Click_1: " + ex);
            }
            finally
            {
                _runningRefactor = false;
                button2.Text = "GO!";
                StopBlinking(button2);
            }
        }


        private void treeView1_AfterCheck(object sender, TreeViewEventArgs e)
        {
            //During processing, checkboxes are locked: cancel the change and restore the previous state.
            if (_runningRefactor)
            {
                treeView1.AfterCheck -= treeView1_AfterCheck;
                e.Node.Checked = !e.Node.Checked;   // ripristina lo stato precedente
                treeView1.AfterCheck += treeView1_AfterCheck;
                return;
            }

            // Avoid recursive calls
            treeView1.AfterCheck -= treeView1_AfterCheck;

            try
            {
                foreach (TreeNode child in e.Node.Nodes)
                {
                    child.Checked = e.Node.Checked;
                }

                // Update parent node based on children's state
                if (e.Node.Parent != null)
                {
                    // If any child is unchecked, uncheck the parent
                    bool allChecked = e.Node.Parent.Nodes.Cast<TreeNode>().All(n => n.Checked);
                    e.Node.Parent.Checked = allChecked;
                }

                _selectedIds.Clear();
                foreach (TreeNode p in treeView1.Nodes)
                    foreach (TreeNode leaf in p.Nodes)
                        if (leaf.Checked && leaf.Tag is int id)
                            _selectedIds.Add(id);
            }
            finally
            {
                // Re-enable the event handler
                treeView1.AfterCheck += treeView1_AfterCheck;
            }
        }


        //Select node and populate WebView1
        private async void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (_runningRefactor)
            {
                MessageBox.Show("Code optimization is running. Wait for its completion", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Ignores parent nodes  
            if (e.Node.Nodes.Count > 0)
                return;

            int idNode = (int)e.Node.Tag;

            //Shows optimized code ONLY if the node is bold (has been optimized)
            bool isBold = e.Node.NodeFont != null && e.Node.NodeFont.Style.HasFlag(FontStyle.Bold);
            ShowObjectCore(idNode, showOptimized: isBold);
        }

        //Shared logic between treeview and dashboard: single entry point to select an object.
        public void SelectObject(int idNode)
        {
            if (_runningRefactor)
            {
                MessageBox.Show("Code optimization is running. Wait for its completion", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            //Shows the optimized code if it exists, otherwise shows the source SQL.
            ShowObjectCore(idNode, showOptimized: true);

            //Blue background
            TreeNode node = FindNodeByTag(treeView1.Nodes, idNode);
            if (node != null)
            {
                treeView1.SelectedNode = node;
                node.EnsureVisible();
                treeView1.Focus();  
            }
        }

        //Searches recursively for the node with Tag (int) == id. Returns null if not found.
        private TreeNode FindNodeByTag(TreeNodeCollection nodes, int id)
        {
            foreach (TreeNode n in nodes)
            {
                if (n.Tag is int tag && tag == id)
                    return n;

                TreeNode found = FindNodeByTag(n.Nodes, id);
                if (found != null)
                    return found;
            }
            return null;
        }

        //Shared body between NodeMouseClick and SelectObject
        private void ShowObjectCore(int idNode, bool showOptimized)
        {
            var row = _state.CodeplexRows.FirstOrDefault(r => r.Id == idNode);
            if (row == null)
            {
                richTextBox1.Text = "-- Object not found --";
                return;
            }

            string schemaName = row.SchemaName;
            string objectName = row.ObjectName;
            string sourceSql = row.SourceSql;
            string optimizedHtml = row.AiOptimized;
            string thread_id_old = row.ThreadId;

            if (!string.IsNullOrEmpty(sourceSql))
            {
                richTextBox1.SelectionIndent = 6;
                richTextBox1.Text = sourceSql;
            }
            else
                richTextBox1.Text = "-- No source SQL found for this object --";

            if (showOptimized && !string.IsNullOrWhiteSpace(optimizedHtml))
            {
                Color inactiveBorder = SystemColors.InactiveBorder;
                string coloreHtml = $"rgb({inactiveBorder.R}, {inactiveBorder.G}, {inactiveBorder.B})";
                string html = optimizedHtml.Trim();
                if (!html.StartsWith("<html>", StringComparison.OrdinalIgnoreCase))
                {
                    html = $@"
                    <html>
                    <head>
                        <style>
                            body {{font-family: Arial; margin: 8px; background-color: {coloreHtml}; font-size: 14px; zoom: 70%; }}
                        </style>
                    </head>
                    <body>
                        <div style='position:fixed; top:0; right:4px; color:gray; background-color:{coloreHtml}; font-family:Arial; font-size:16px; padding:2px 4px; z-index:1000;'>Optimized code</div>
                        {html}
                    </body>
                    </html>";
                }

                ResetWebView2();
                webView21.CoreWebView2?.NavigateToString(html);
                _idnode = idNode;

                //If expired (404), RunAgentConversation injects the context and start fresh
                if (_responseClient != null)
                {
                    _previousResponseId = thread_id_old;
                    _lostSourceSql = sourceSql ?? "";
                    _lostOptimizedHtml = optimizedHtml ?? "";
                    _threadContextLost = false;
                }
                else
                {
                    // Not connected
                    _previousResponseId = null;
                    _threadContextLost = false;
                    _lostSourceSql = "";
                    _lostOptimizedHtml = "";
                }
            }
            else
            {
                ResetWebView2();
                string appendHtml = $"<div style='margin:5px;font-size:14px;color:#666;'>Selected: {schemaName}.{objectName}</div>";
                string script = $@"
                                var contentDiv = document.getElementById('content');
                                if (!contentDiv) {{
                                    contentDiv = document.createElement('div');
                                    contentDiv.id = 'content';
                                    document.body.appendChild(contentDiv);
                                }}
                                var nuovoDiv = document.createElement('div');
                                nuovoDiv.innerHTML = `{appendHtml.Replace("`", "\\`")}`;
                                contentDiv.appendChild(nuovoDiv);
                                window.scrollTo({{ top: document.body.scrollHeight, behavior: 'smooth' }});";

                webView21.CoreWebView2.ExecuteScriptAsync(script);
                _previousResponseId = null;
                _idnode = 0;
                _threadContextLost = false;
            }
        }


        private void ResetAgentContext()
        {
            _previousResponseId = null;
            _threadContextLost = false;
            _lostSourceSql = "";
            _lostOptimizedHtml = "";
        }


        private async Task<string> ExtractSqlFromWebView2Async()
        {
            if (webView21?.CoreWebView2 == null)
                return string.Empty;

            string jsCode = @"
                (function() {
                    const pres = document.querySelectorAll('pre');
                    if (pres.length) return pres[pres.length - 1].textContent;
                    const codes = document.querySelectorAll('code');
                    if (codes.length) return codes[codes.length - 1].textContent;
                    return '';
                })();";

            string result = await webView21.CoreWebView2.ExecuteScriptAsync(jsCode);

            //The result is returned as a JSON string (with double quotes), so we need to decode it.
            if (string.IsNullOrEmpty(result) || result == "null")
                return string.Empty;

            //Decode the JSON string and remove outer quotes
            string sql = System.Text.Json.JsonSerializer.Deserialize<string>(result);
            sql = System.Net.WebUtility.HtmlDecode(sql).Trim();

            return sql;
        }//function


        private void treeView1_DrawNode(object sender, DrawTreeNodeEventArgs e)
        {
            if (e.Node == null)
                return;

            e.DrawDefault = false;

            TreeView tree = (TreeView)sender;
            Graphics g = e.Graphics;

            Font font = e.Node.NodeFont ?? tree.Font;
            bool selected = (e.State & TreeNodeStates.Selected) != 0;

            //Checkbox graphics 
            int checkBoxWidth = tree.CheckBoxes ? 8 : 0;
            int textLeft = e.Bounds.Left + checkBoxWidth;

            //Background after checkbox
            using (var bg = new SolidBrush(selected ? SystemColors.Highlight : tree.BackColor))
            {
                g.FillRectangle(
                    bg,
                    new Rectangle(
                        textLeft,
                        e.Bounds.Top,
                        tree.ClientSize.Width - textLeft,
                        e.Bounds.Height
                    )
                );
            }

            Color textColor = selected ? SystemColors.HighlightText : tree.ForeColor;

            //Numeric metric column
            int numberColumnX = tree.ClientSize.Width - TREE_NUMBER_COLUMN_WIDTH - TREE_NUMBER_RIGHT_MARGIN;

            //Text space
            if (numberColumnX < textLeft + TREE_MIN_TEXT_WIDTH)
                numberColumnX = textLeft + TREE_MIN_TEXT_WIDTH;

            //text rectangle
            Rectangle textRect = new Rectangle(textLeft, e.Bounds.Top, Math.Max(TREE_MIN_TEXT_WIDTH, numberColumnX - textLeft - TREE_COLUMN_GAP), e.Bounds.Height);
            TextRenderer.DrawText(g, e.Node.Text, font, textRect, textColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);

            //number
            if (e.Node.Nodes.Count == 0 && _treeNodeNumbers.TryGetValue(e.Node, out long value))
            {
                Rectangle numberRect = new Rectangle(numberColumnX, e.Bounds.Top, TREE_NUMBER_COLUMN_WIDTH, e.Bounds.Height);
                TextRenderer.DrawText(g, value.ToString(), font, numberRect, Color.DimGray, TextFormatFlags.Right | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding
                );
            }
        }


        public void SetTreeNodeNumber(TreeNode node, long value)
        {
            _treeNodeNumbers[node] = value;
            treeView1.Invalidate(); // forza il ridisegno
        }


        private async void Combo_SelectionChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_state.ConnectionString))
                return;

            string metric = ((KeyValuePair<string, string>)comboMetric.SelectedItem).Value;
            await LoadTreeViewAsync(metric);
        }


        // treeView1 populate (Thread_id → bold)
        public Task LoadTreeViewAsync(string metricColumn)
        {
            treeView1.BeginUpdate();
            treeView1.AfterCheck -= treeView1_AfterCheck;
            treeView1.Nodes.Clear();
            _treeNodeNumbers.Clear();

            var categoryNodes = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);

            // equivalent of: ORDER BY typedesc, <metric> DESC, schemaname, objectname
            var ordered = _state.CodeplexRows
                .OrderBy(r => r.TypeDesc)
                .ThenByDescending(r => Metric(r, metricColumn))
                .ThenBy(r => r.SchemaName)
                .ThenBy(r => r.ObjectName);

            foreach (var row in ordered)
            {
                string type = (row.TypeDesc ?? string.Empty).TrimEnd();
                long metric = Metric(row, metricColumn);
                bool hasThread = !string.IsNullOrEmpty(row.ThreadId);

                if (!categoryNodes.TryGetValue(type, out TreeNode parent))
                {
                    parent = new TreeNode(type + " ");
                    categoryNodes[type] = parent;
                    treeView1.Nodes.Add(parent);
                }

                string label = string.IsNullOrEmpty(row.SchemaName)
                    ? (row.ObjectName ?? string.Empty)
                    : $"{row.SchemaName}.{row.ObjectName}";

                TreeNode child = new TreeNode(label) { Tag = row.Id };
                if (hasThread)
                    child.NodeFont = new Font(treeView1.Font.FontFamily, treeView1.Font.Size, FontStyle.Bold);
                child.Checked = _selectedIds.Contains(row.Id);
                parent.Nodes.Add(child);
                SetTreeNodeNumber(child, metric);
            }

            treeView1.CollapseAll();
            treeView1.AfterCheck += treeView1_AfterCheck;
            treeView1.EndUpdate();
            return Task.CompletedTask;
        }

        // Helper 
        private static long Metric(CodeplexRow r, string m) => m switch
        {
            "cpu" => r.Cpu ?? 0L,
            "elapsed" => r.Elapsed ?? 0L,
            "reads" => r.Reads ?? 0L,
            _ => r.Executions ?? 0L,
        };

        //SETTINGS click
        private void label7_Click(object sender, EventArgs e)
        {
            using (var frm = new FormSettings(_state))
            {
                //Read state before opening the dialog
                label3.Text = _state.SqlServer + (string.IsNullOrWhiteSpace(_state.SqlServerVersion) ? "" : "  (ver. " + _state.SqlServerVersion + ")");
                label4.Text = _state.SqlDatabase;

                // if already connected to Foundry pass current values to FormSettings
                if (_projectClient != null)
                {
                    frm.AttachFoundryRuntime(_credential, _projectClient, _state.SelectedAgentName);
                }

                frm.StartPosition = FormStartPosition.CenterParent;

                if (frm.ShowDialog(this) == DialogResult.OK)
                {
                    //After closed Settings, I get vital variables values
                    _credential = frm.Credential;
                    _projectClient = frm.ProjectClient;
                    _state.SelectedAgentName = frm.SelectedAgentName;
                    _state.SelectedAgentId = frm.SelectedAgentName;

                    if (_projectClient != null && !string.IsNullOrWhiteSpace(_state.SelectedAgentName))
                        _responseClient = _projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgent(_state.SelectedAgentName);

                    //updates UI reading from the state
                    label8.Text = "Foundry Agent : " + _state.SelectedAgentName;
                    label9.Text = "AI Model          : " + _state.SelectedAgentModel;

                    label3.Text = _state.SqlServer + (string.IsNullOrWhiteSpace(_state.SqlServerVersion) ? "" : "  (ver. " + _state.SqlServerVersion + ")");
                    label4.Text = _state.SqlDatabase;

                    ResetAgentContext();
                    UpdateSettingsIndicator();
                }
            }
        }


        //Cleanup code panels (original + optimized). Used when the session is replaced.
        public void ClearCodePanels()
        {
            richTextBox1.Clear();
            ResetWebView2();
        }


        public void UpdateDatabaseLabels(string serverName, string dbName)
        {
            label3.Text = serverName + (string.IsNullOrWhiteSpace(_state.SqlServerVersion) ? "" : "  (ver. " + _state.SqlServerVersion + ")");
            label4.Text = dbName;
        }


        public void ApplyFoundryRuntime(InteractiveBrowserCredential credential, AIProjectClient projectClient, string agentName, string agentModel)
        {
            _credential = credential;
            _projectClient = projectClient;
            _state.SelectedAgentName = agentName;
            _state.SelectedAgentModel = agentModel;   

            if (_projectClient != null && !string.IsNullOrWhiteSpace(agentName))
                _responseClient = _projectClient.ProjectOpenAIClient.GetProjectResponsesClientForAgent(agentName);

            label8.Text = "Foundry Agent : " + _state.SelectedAgentName;
            label9.Text = "AI Model          : " + _state.SelectedAgentModel;

            ResetAgentContext();
            UpdateSettingsIndicator();
        }


        private void InitializeTreeViewContextMenu()
        {
            _treeContextMenu = new ContextMenuStrip();
            _treeContextMenu.BackColor = Color.FromArgb(40, 40, 40);
            _treeContextMenu.ForeColor = Color.White;

            var refactorItem = new ToolStripMenuItem("Refactor the SQL code");
            refactorItem.Click += RefactorMenuItem_Click;
            _treeContextMenu.Items.Add(refactorItem);

            var selectItem = new ToolStripMenuItem("Select item");    
            selectItem.Click += SelectItemMenuItem_Click;             
            _treeContextMenu.Items.Add(selectItem);
        }

        private void SelectItemMenuItem_Click(object sender, EventArgs e)
        {
            if (_rightClickedNode == null)
                return;

            _rightClickedNode.Checked = true;
        }


        private void treeView1_NodeMouseClick_ContextMenu(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (_runningRefactor)
            {
                return;
            }

            //Right click only
            if (e.Button != MouseButtons.Right)
                return;

            //Leaves nodes only
            if (e.Node == null || e.Node.Nodes.Count > 0)
                return;

            //Selects the node 
            treeView1.SelectedNode = e.Node;
            _rightClickedNode = e.Node;

            // Show context menu
            _treeContextMenu.Show(treeView1, e.Location);
        }


        private async void RefactorMenuItem_Click(object sender, EventArgs e)
        {
            if (_rightClickedNode == null)
                return;

            //Check Agent configured
            if (!EnsureAgentReady()) return;

            if (_runningRefactor)
            {
                MessageBox.Show("Code optimization is running. Wait for its completion", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int idNode = (int)_rightClickedNode.Tag;

            //Retrieves the SQL from the row in memory
            string sqlSource = _state.CodeplexRows.FirstOrDefault(r => r.Id == idNode)?.SourceSql;

            if (string.IsNullOrWhiteSpace(sqlSource))
            {
                MessageBox.Show("No SQL source found for this object.");
                return;
            }

            //Too large code for the model (768k char limit in 'content'): warn and stop.
            if (sqlSource.Length > MaxAzureMessageChars)
            {
                MessageBox.Show(
                    $"The object source is too large for the model: {sqlSource.Length:N0} characters (limit {MaxAzureMessageChars:N0}).\nOptimize it manually or split it into smaller parts.",
                    "Object too large",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            //Warns if the DB connection is missing (only once)
            if (string.IsNullOrWhiteSpace(_state.ConnectionString))
            {
                MessageBox.Show("SQL Server is not connected. The optimization will be performed without schema or index information and will be based on SQL syntax only.", "", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            //Enligths the node in bold to indicate it's being processed
            _rightClickedNode.NodeFont = new Font(treeView1.Font.FontFamily, treeView1.Font.Size + 0.5f, FontStyle.Bold);
            treeView1.Refresh();

            //Graphics start button3
            button3.Text = "⏳ Optimizing...";
            button3.Font = new Font(button3.Font, FontStyle.Bold);
            button3.ForeColor = Color.LightGreen;
            StartBlinking(button3);
            _runningRefactor = true;

            try
            {
                // Reset Agent context
                ResetAgentContext();
                AggiungiContenutoHtmlSenzaScroll($"🔧 Refactoring object: {_rightClickedNode.Text}");

                //Calls the Agent 
                string response = await RunAgentConversation("Analyze, optimize and rewrite this T-SQL code: " + sqlSource);

                //Extracts 4 scores <!--IMPACT ...--> and removes it from the text.
                response = ImpactExtractor.ExtractAndStrip(response, out var impact);

                string htmlClean = response.Replace("```html", "").Replace("```", "").Trim();
                htmlClean = "<div>" + htmlClean + $"[Model:{_state.SelectedAgentModel}] time: {FormatElapsed(_lastAgentElapsed)}" + "</div>";

                AggiungiContenutoHtmlScroll(htmlClean);

                // Save in memory
                var row = _state.CodeplexRows.FirstOrDefault(r => r.Id == idNode);
                if (row != null)
                {
                    row.AiOptimized = htmlClean;
                    row.ThreadId = _previousResponseId;
                    ImpactExtractor.Apply(row, impact);
                }
            }
            catch (Exception ex)
            {
                AggiungiContenutoHtmlSenzaScroll($"💥 Refactoring failed: {ex.Message}");
                AIUtility.TraceLog("RefactorMenuItem_Click: " + ex);
            }
            finally
            {
                //Graphics end button3
                button3.Text = "Optimize window code";
                button3.Font = new Font(button3.Font, FontStyle.Regular);
                button3.ForeColor = Color.White;
                StopBlinking(button3);
                _runningRefactor = false;
            }
        }


        private void LayoutSplitArea()
        {
            if (_splitter == null) return;

            int WW = this.ClientSize.Width;

            int leftStart = (_splitterTree != null) ? _splitterTree.Right : treeView1.Right + 2;
            int rightEnd = WW - 12;
            int avail = rightEnd - leftStart;

            // richTextBox1 (left)
            richTextBox1.Left = leftStart;
            richTextBox1.Width = (int)(avail * _splitRatio) - _splitter.Width / 2;

            // splitter 
            _splitter.Top = richTextBox1.Top;
            _splitter.Height = richTextBox1.Height;
            _splitter.Left = richTextBox1.Right;

            // webView21 
            webView21.Left = _splitter.Right;
            webView21.Width = rightEnd - webView21.Left;

            // label upper left corner
            if (_lblOriginalCode != null)
            {
                int x = richTextBox1.Right - _lblOriginalCode.Width - SystemInformation.VerticalScrollBarWidth - 2;
                _lblOriginalCode.Location = new Point(x, richTextBox1.Top + 2);
                _lblOriginalCode.BringToFront();
            }
        }


        private void Splitter_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_draggingSplit) return;

            int WW = this.ClientSize.Width;
            int leftStart = (_splitterTree != null) ? _splitterTree.Right : treeView1.Right + 2;
            int rightEnd = WW - 12;
            int mouseX = _splitter.Left + e.X;     

            double ratio = (double)(mouseX - leftStart) / (rightEnd - leftStart);
            if (ratio < 0.15) ratio = 0.15;           //Left panel
            if (ratio > 0.85) ratio = 0.85;           //Right panel
            _splitRatio = ratio;

            LayoutSplitArea();
        }


        private void LayoutTreeArea()
        {
            if (_splitterTree == null) return;

            int WW = this.ClientSize.Width;

            // larghezza tree da ratio, con limiti min/max di sicurezza
            int treeWidth = (int)(WW * _treeRatio);
            int minTree = (int)(WW * 0.08);
            int maxTree = (int)(WW * 0.40);
            if (treeWidth < minTree) treeWidth = minTree;
            if (treeWidth > maxTree) treeWidth = maxTree;

            treeView1.Left = 6;
            treeView1.Width = treeWidth;
            comboMetric.Width = treeWidth;   // la combo sopra il tree resta allineata

            _splitterTree.Left = treeView1.Right;
            _splitterTree.Top = treeView1.Top;
            _splitterTree.Height = treeView1.Height;

            // richTextBox1 | splitter destro | webView21 si riflettono da treeView1.Right
            LayoutSplitArea();

            // il bottone sotto il pannello codice segue lo spostamento orizzontale
            button3.Left = richTextBox1.Left;
        }


        private void SplitterTree_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_draggingTree) return;

            int WW = this.ClientSize.Width;
            int mouseX = _splitterTree.Left + e.X;

            double ratio = (double)(mouseX - treeView1.Left) / WW;
            if (ratio < 0.08) ratio = 0.08;
            if (ratio > 0.40) ratio = 0.40;
            _treeRatio = ratio;

            LayoutTreeArea();
        }

        private void InitializeSessionIcons()
        {
            _topBarTips = new ToolTip { InitialDelay = 300, ReshowDelay = 100 };
            _topBarTips.SetToolTip(label7, "Settings");

            // Gears icons. Glyph MDL2: Save = E74E, OpenFile = E8E5, Impact/Chart = E9D9
            _lblOpenSession = CreateTopBarIcon("\uE8E5", "Open session…");
            _lblSaveSession = CreateTopBarIcon("\uE74E", "Save session…");
            _lblImpact = CreateTopBarIcon("\uE9D9", "Dashboard");

            const int gap = 22;
            _lblSaveSession.Top = label7.Top;
            _lblOpenSession.Top = label7.Top;
            _lblImpact.Top = label7.Top;

            _lblSaveSession.Left = label7.Left - _lblSaveSession.Width - gap;
            _lblOpenSession.Left = _lblSaveSession.Left - _lblOpenSession.Width - gap;
            _lblImpact.Left = _lblOpenSession.Left - _lblImpact.Width - gap;   // a sinistra di Open

            _lblSaveSession.Click += async (s, e) => await SaveSessionAsync();
            _lblOpenSession.Click += async (s, e) => await OpenSessionAsync();
            _lblImpact.Click += (s, e) => OpenImpactDashboard();
        }


        private Label CreateTopBarIcon(string glyph, string tip)
        {
            var lbl = new Label
            {
                AutoSize = true,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe MDL2 Assets", 30f),
                ForeColor = Color.White,
                BackColor = panelTop.BackColor,
                Text = glyph,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            panelTop.Controls.Add(lbl);
            lbl.Parent = panelTop;
            _topBarTips.SetToolTip(lbl, tip);
            lbl.MouseEnter += (s, e) => lbl.ForeColor = Color.LightGray;   
            lbl.MouseLeave += (s, e) => lbl.ForeColor = Color.White;
            return lbl;
        }


        // Server/Database shown into the dashboard.
        public string ImpactServer => !string.IsNullOrWhiteSpace(_state.LoadedFileServer) ? _state.LoadedFileServer : _state.SqlServer;
        public string ImpactDatabase => !string.IsNullOrWhiteSpace(_state.LoadedFileDatabase) ? _state.LoadedFileDatabase : _state.SqlDatabase;

        private void OpenImpactDashboard()
        {
            //If Dashboard is already open, take it upfront
            if (_formImpact != null && !_formImpact.IsDisposed)
            {
                _formImpact.RefreshData(_state.CodeplexRows);

                //If it was minimized, restore it to normal window.
                if (_formImpact.WindowState == FormWindowState.Minimized)
                    _formImpact.WindowState = FormWindowState.Normal;

                _formImpact.BringToFront();
                _formImpact.Activate();

                // BringToFront/Activate 
                _formImpact.TopMost = true;
                _formImpact.TopMost = false;

                return;
            }

            //First open (or after close): create the window, non-modal, and show it.
            _formImpact = new FormImpact(this, _state.CodeplexRows);
            _formImpact.Show(this);   // Show (non ShowDialog) = non modale
        }


        private async Task SaveSessionAsync()
        {
            if (_runningRefactor) { MessageBox.Show("Wait for the current optimization to finish."); return; }

            using var sfd = new SaveFileDialog
            {
                Filter = "AI SQL session (*.aisql)|*.aisql|JSON (*.json)|*.json",
                FileName = $"{_state.SqlDatabase}_{DateTime.Now:yyyyMMdd_HHmm}.aisql"
            };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                Cursor = Cursors.WaitCursor;
                // Data source to be written in the file.  
                string serverToSave = !string.IsNullOrWhiteSpace(_state.LoadedFileServer) ? _state.LoadedFileServer : _state.SqlServer;
                string databaseToSave = !string.IsNullOrWhiteSpace(_state.LoadedFileDatabase) ? _state.LoadedFileDatabase : _state.SqlDatabase;
                string versionToSave = !string.IsNullOrWhiteSpace(_state.LoadedFileSqlServerVersion) ? _state.LoadedFileSqlServerVersion : _state.SqlServerVersion;
                await SessionStorage.SaveAsync(_state.CodeplexRows, serverToSave, databaseToSave, versionToSave, _state.strColumnTypesList, _state.strIndexesList, sfd.FileName);
                MessageBox.Show("Session saved.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving session: " + ex.Message, "Save",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor = Cursors.Default; }
        }

        private async Task OpenSessionAsync()
        {
            if (_runningRefactor) { MessageBox.Show("Wait for the current optimization to finish."); return; }

            using var ofd = new OpenFileDialog
            {
                Filter = "AI SQL session (*.aisql;*.json)|*.aisql;*.json|All files (*.*)|*.*"
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            //Check if there are unsaved changes in the current session (CodeplexRows not empty).
            if (_state.CodeplexRows.Count > 0 &&
                MessageBox.Show("Loading the file will replace the current session content. Continue?",
                "Open session", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
                return;

            try
            {
                Cursor = Cursors.WaitCursor;
                // 1) Read the session file      
                var session = await SessionStorage.LoadAsync(ofd.FileName);

                // 2) info on Server/DB from file, before loading the session. 
                string serverInfo = string.IsNullOrWhiteSpace(session.SqlServer) ? "(not specified)" : session.SqlServer;
                string dbInfo = string.IsNullOrWhiteSpace(session.SqlDatabase) ? "(not specified)" : session.SqlDatabase;

                string extra;
                MessageBoxIcon icon;
                if (string.IsNullOrWhiteSpace(_state.ConnectionString))
                {
                    // 1) No active connection
                    extra = "SQL Server is not connected. The optimization will be performed without schema or index information and will be based on SQL syntax only.";
                    icon = MessageBoxIcon.Warning;
                }
                else if (string.Equals(_state.SqlServer?.Trim(), session.SqlServer?.Trim(), StringComparison.OrdinalIgnoreCase)
                      && string.Equals(_state.SqlDatabase?.Trim(), session.SqlDatabase?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    // 2) Connected to the same server and database as the file
                    extra = "You are currently connected to the SQL server and Database of the loading file. You can refactor at best";
                    icon = MessageBoxIcon.Information;
                }
                else
                {
                    // 3) Connected to a different server or database than the file
                    extra = "You are currently connected to a different Server or Database! I suggest to change the connection and reload the file";
                    icon = MessageBoxIcon.Warning;
                }

                MessageBox.Show(
                    $"This session refers to:\n\nSQL Server : {serverInfo}\nDatabase   : {dbInfo}\n\n{extra}",
                    "Session source",
                    MessageBoxButtons.OK,
                    icon);

                // 3) Load in session: update state and UI.
                _state.CodeplexRows.Clear();
                _state.CodeplexRows.AddRange(session.Rows);
                _state.LoadedFileServer = session.SqlServer ?? "";
                _state.LoadedFileDatabase = session.SqlDatabase ?? "";
                _state.LoadedFileSqlServerVersion = session.SqlServerVersion ?? "";

                // The "live" version and schema (used for agent and label) are taken from the file ONLY if you are NOT connected: if there is a live connection, the scanned ones prevail.
                if (string.IsNullOrWhiteSpace(_state.ConnectionString))
                {
                    _state.SqlServerVersion = session.SqlServerVersion ?? "";
                    _state.strColumnTypesList = session.StrColumnTypesList ?? "";
                    _state.strIndexesList = session.StrIndexesList ?? "";
                }

                //Cleanup UI
                richTextBox1.Clear();
                ResetWebView2();
                await LoadTreeViewAsync("executions");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading session: " + ex.Message, "Open session",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor = Cursors.Default; }
        }

    } //class
}

