using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;

namespace AISQLOptimizer
{
    /// <summary>
    /// Dashboard "Optimization impact": not modal, single instance window. 
    /// </summary>
    public sealed class FormImpact : Form
    {
        private readonly Form1 _mainForm;
        private readonly WebView2 _web;
        private List<CodeplexRow> _rows; 
        private bool _coreReady;

        public FormImpact(Form1 mainForm, IEnumerable<CodeplexRow> rows)
        {
            _mainForm = mainForm;
            _rows = (rows ?? Enumerable.Empty<CodeplexRow>()).ToList();

            Text = "Optimization impact";
            StartPosition = FormStartPosition.CenterScreen;

            // 60% width e 80% height of main window
            Width = (int)(mainForm.Width * 0.60);
            Height = (int)(mainForm.Height * 0.80);

            MinimumSize = new System.Drawing.Size(820, 480);

            _web = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(_web);

            Load += async (s, e) => await InitAsync();
        }

        private async Task InitAsync()
        {
            await _web.EnsureCoreWebView2Async(null);
            _coreReady = true;

            //One row click = returns the idNode of the clicked object, which is sent to the main form to select it in the treeview.
            _web.CoreWebView2.WebMessageReceived += (s, e) =>
            {
                string msg = e.TryGetWebMessageAsString();
                if (int.TryParse(msg, out int idNode))
                {
                    _mainForm.SelectObject(idNode);
                    _mainForm.Activate();   
                }
            };

            Render();
        }

        /// <summary>
        ///Data load. Main form calls this when reopening the dashboard, after new optimizations.
        /// </summary>
        public void RefreshData(IEnumerable<CodeplexRow> rows)
        {
            _rows = (rows ?? Enumerable.Empty<CodeplexRow>()).ToList();
            if (_coreReady)
                Render();
        }

        private void Render()
        {
            //Only refactored objects are shown in the dashboard.
            var items = _rows
                .Where(r => !string.IsNullOrWhiteSpace(r.AiOptimized))
                .Select(r => new
                {
                    id = r.Id,
                    name = string.IsNullOrWhiteSpace(r.SchemaName)
                        ? (r.ObjectName ?? "")
                        : $"{r.SchemaName}.{r.ObjectName}",
                    type = TypeLabel(r.TypeDesc),
                    sec = r.SecurityScore,
                    perf = r.PerformanceScore,
                    comp = r.ComplianceScore,
                    dep = r.DeprecationScore
                })
                .ToList();

            string json = JsonSerializer.Serialize(items);
            string html = HtmlTemplate
                .Replace("/*DATA*/", json)
                .Replace("/*SERVER*/", JsonSerializer.Serialize(_mainForm.ImpactServer ?? ""))
                .Replace("/*DATABASE*/", JsonSerializer.Serialize(_mainForm.ImpactDatabase ?? ""));

            _web.CoreWebView2.NavigateToString(html);
        }

        private static string TypeLabel(string typeDesc)
        {
            if (string.IsNullOrEmpty(typeDesc)) return "";
            string t = typeDesc.ToUpperInvariant();
            if (t.Contains("PROCEDURE")) return "SP";
            if (t.Contains("FUNCTION")) return "FN";
            if (t.Contains("VIEW")) return "VIEW";
            if (t.Contains("TRIGGER")) return "TRG";
            return typeDesc;
        }

        // ============================================================
        //  Template HTML/JS. Placeholder /*DATA*/ is replaced with the JSON array of rows.
        // ============================================================ 
        private const string HtmlTemplate = @"
<!DOCTYPE html>
<html>
<head>
<meta charset='utf-8'>
<style>
  * { box-sizing: border-box; }
  body { font-family: 'Segoe UI', Arial, sans-serif; margin: 0; padding: 24px 28px; color: #1c1c1c; background: #ffffff; }
.head { display:flex; align-items:center; justify-content:space-between; margin-bottom:18px; }
  .head h1 { font-size:22px; font-weight:600; margin:0; line-height:1; }
  .headinfo { display:flex; flex-direction:column; align-items:flex-end; gap:3px; line-height:1.2; }
  .head .sub { color:#8a8a8a; font-size:12.5px; }
  .dbline { font-size:12.5px; color:#8a8a8a; }
  .dbval { font-family:'Cascadia Code','Consolas',monospace; color:#1c1c1c; font-weight:600; }
  .cards { display:grid; grid-template-columns:repeat(4,1fr); gap:16px; margin-bottom:22px; }
  .card { background:#f3f1ea; border-radius:10px; padding:8px 16px; }
  .card .lbl { font-size:17px; font-weight:700; color:#2563eb; }
  .card .num { font-size:30px; font-weight:600; margin:4px 0 2px; }
  .card .cap { font-size:13px; color:#8a8a8a; }
  table { width:100%; border-collapse:collapse; }
  thead th { font-size:14px; color:#555; font-weight:500; padding:10px 8px; border-bottom:1px solid #e7e4dc; text-align:center; }
  thead th.obj { text-align:left; }
  thead th.tot { text-align:right; }
  thead th.sortable { cursor:pointer; user-select:none; }
  thead th.sortable:hover { color:#1c1c1c; }
  .arrow { margin-left:4px; font-size:11px; color:#555; }
  tbody td { padding:2px 8px; border-bottom:1px solid #f0eee8; text-align:center; vertical-align:middle; }
  tbody tr { cursor:pointer; }
  tbody tr:hover { background:#faf9f5; }
  td.obj { text-align:left; }
  .objname { font-family:'Cascadia Code','Consolas',monospace; font-size:13px; }
  .badge { font-size:10px; color:#8a8a8a; border:1px solid #d8d5cc; border-radius:4px; padding:1px 5px; margin-left:8px; vertical-align:middle; }
  .icon { color:#9a9a9a; margin-right:6px; }
  .pill { display:inline-block; min-width:34px; padding:2px 0; border-radius:8px; font-weight:600; font-size:12px; }
  .dash { color:#c2c2c2; }
  .barwrap { display:flex; align-items:center; gap:10px; justify-content:flex-end; }
  .bartrack { width:170px; height:14px; background:#f1efe8; border-radius:7px; overflow:hidden; display:flex; }
  .barseg { height:100%; }
  .barnum { min-width:24px; text-align:right; font-weight:600; font-size:14px; }
  .empty { color:#8a8a8a; font-size:15px; padding:40px 0; text-align:center; }
  .legend { margin-top:18px; font-size:13px; color:#8a8a8a; display:flex; align-items:center; gap:10px; }
  .legend .sw { display:inline-block; width:24px; height:16px; border-radius:8px; vertical-align:middle; }
</style>
</head>
<body>
<div class='head'>
    <h1>Optimization impact</h1>
    <div class='headinfo'>
      <div class='dbline' id='dbline'>SQL Server <span class='dbval' id='srv'></span> &nbsp;&#183;&nbsp; Database <span class='dbval' id='db'></span></div>
      <div class='sub' id='subhead'></div>
    </div>
  </div>
  <div class='cards' id='cards'></div>
  <table>
    <thead>
      <tr>
        <th class='obj'>Object</th>
        <th class='sortable' onclick=""sortBy('sec')"">Security<span class='arrow' id='arr-sec'></span></th>
        <th class='sortable' onclick=""sortBy('perf')"">Perf.<span class='arrow' id='arr-perf'></span></th>
        <th class='sortable' onclick=""sortBy('comp')"">Compliance<span class='arrow' id='arr-comp'></span></th>
        <th class='sortable' onclick=""sortBy('dep')"">Deprec.<span class='arrow' id='arr-dep'></span></th>
        <th class='sortable tot' onclick=""sortBy('total')"">Total impact<span class='arrow' id='arr-total'></span></th>
      </tr>
    </thead>
    <tbody id='tbody'></tbody>
  </table>
  <div id='emptymsg'></div>
<div class='legend'>
    <span>Optimizations applied:</span>
    <span>few</span>
    <span style='display:inline-flex; border-radius:4px; overflow:hidden;'>
      <span style='width:16px;height:14px;background:#f7c1c1;'></span>
      <span style='width:16px;height:14px;background:#f09595;'></span>
      <span style='width:16px;height:14px;background:#e24b4a;'></span>
      <span style='width:16px;height:14px;background:#cf3b3a;'></span>
      <span style='width:16px;height:14px;background:#a32d2d;'></span>
      <span style='width:16px;height:14px;background:#791f1f;'></span>
    </span>
    <span>many</span>
    <span><span class='dash'>&#8211;</span> none</span>
  </div>

<script>
  var DATA = /*DATA*/;
  var SERVER = /*SERVER*/;
  var DATABASE = /*DATABASE*/;

  // Header: scrive server/database con textContent  
  (function () {
    var s = document.getElementById('srv'); if (s) s.textContent = SERVER;
    var d = document.getElementById('db');  if (d) d.textContent = DATABASE;
    var line = document.getElementById('dbline');
    if (line && !SERVER && !DATABASE) line.style.display = 'none';
  })();

// Scala monocromatica: 6 tonalita' di rosso (chiaro -> scuro) = intensita' crescente.
  var RED_BASE = '#e24b4a';
  var RED_BUCKETS = ['#f7c1c1', '#f09595', '#e24b4a', '#cf3b3a', '#a32d2d', '#791f1f'];
  var RED_TEXT_DARK = '#501313';    // testo sui toni chiari (bucket 0-2)
  var RED_TEXT_LIGHT = '#fceaea';   // testo sui toni scuri (bucket 3-5)

  function bucket(v) { if (v <= 0) return -1; if (v <= 1) return 0; if (v <= 2) return 1; if (v <= 3) return 2; if (v <= 4) return 3; if (v <= 6) return 4; return 5; }

function pill(v) {
    if (v <= 0) return ""<span class='dash'>&#8211;</span>"";
    var b = bucket(v);
    var bg = RED_BUCKETS[b];
    var fg = (b >= 3) ? RED_TEXT_LIGHT : RED_TEXT_DARK;
    return ""<span class='pill' style='background:"" + bg + "";color:"" + fg + ""'>"" + v + ""</span>"";
  }

  function total(r) { return r.sec + r.perf + r.comp + r.dep; }

  // Stato di ordinamento. Default: impatto totale decrescente (come prima).
  var sortKey = 'total';
  var sortDir = -1;   // -1 = decrescente, 1 = crescente

  function val(r, k) { return k === 'total' ? total(r) : r[k]; }

  function sortBy(key) {
    if (sortKey === key) sortDir = -sortDir;    
    else { sortKey = key; sortDir = -1; }       
    render();
  }

  function updateArrows() {
    ['sec','perf','comp','dep','total'].forEach(function(k){
      var el = document.getElementById('arr-' + k);
      if (el) el.textContent = (k === sortKey) ? (sortDir === 1 ? '\u25B2' : '\u25BC') : '';
    });
  }

function bar(r, maxTotal) {
    var t = total(r);
    var widthPct = maxTotal > 0 ? Math.round((t / maxTotal) * 100) : 0;
    var fill = (t > 0) ? ""<div class='barseg' style='width:100%;background:"" + RED_BASE + ""'></div>"" : '';
    return ""<div class='barwrap'><div class='bartrack' style='width:"" + Math.max(widthPct,2) + ""%'>"" + fill +
           ""</div><span class='barnum'>"" + t + ""</span></div>"";
  }

  function render() {
    var rows = DATA.slice().sort(function(a,b){
      var d = val(a, sortKey) - val(b, sortKey);
      if (d === 0) d = total(a) - total(b);    
      return sortDir * d;
    });
    updateArrows();

    if (!rows.length) {
      document.getElementById('subhead').textContent = '0 objects refactored';
      document.getElementById('cards').innerHTML = '';
      document.getElementById('emptymsg').innerHTML =
        ""<div class='empty'>No objects refactored yet. Optimize something from the main window.</div>"";
      return;
    }

    var maxTotal = Math.max.apply(null, rows.map(total));
    var sums = { sec:0, perf:0, comp:0, dep:0 };
    rows.forEach(function(r){ sums.sec+=r.sec; sums.perf+=r.perf; sums.comp+=r.comp; sums.dep+=r.dep; });

    document.getElementById('subhead').textContent =
      rows.length + ' objects refactored \u00b7 sorted by total impact';

    var cards = [
      ['Security', sums.sec, RED_BASE],
      ['Performance', sums.perf, RED_BASE],
      ['Compliance', sums.comp, RED_BASE],
      ['Deprecations', sums.dep, RED_BASE]
    ];

    document.getElementById('cards').innerHTML = cards.map(function(c){
    return ""<div class='card'><div class='lbl'>"" + c[0] + ""</div><div class='num'>"" + c[1] +
             ""</div><div class='cap'>issues resolved</div></div>"";
    }).join('');

    document.getElementById('tbody').innerHTML = rows.map(function(r){
      var glyph = r.type === 'FN' ? '&#402;<sub>x</sub>' : '&#128462;';
      return ""<tr onclick='pick("" + r.id + "")'>"" +
        ""<td class='obj'><span class='icon'>"" + glyph + ""</span><span class='objname'>"" + r.name +
          ""</span><span class='badge'>"" + r.type + ""</span></td>"" +
        ""<td>"" + pill(r.sec,'sec') + ""</td>"" +
        ""<td>"" + pill(r.perf,'perf') + ""</td>"" +
        ""<td>"" + pill(r.comp,'comp') + ""</td>"" +
        ""<td>"" + pill(r.dep,'dep') + ""</td>"" +
        ""<td>"" + bar(r, maxTotal) + ""</td>"" +
      ""</tr>"";
    }).join('');
  }

  function pick(id) {
    if (window.chrome && window.chrome.webview) window.chrome.webview.postMessage(String(id));
  }

  render();
</script>
</body>
</html>";
    }
}
