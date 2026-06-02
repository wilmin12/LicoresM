import React from 'react';

// ── Helpers ──────────────────────────────────────────────────────────────────
const fmt = (val, decimals = 2) => {
  const n = Number(val ?? 0);
  if (isNaN(n)) return '';
  return n.toLocaleString('en-US', { minimumFractionDigits: decimals, maximumFractionDigits: decimals });
};

// ── Atoms ─────────────────────────────────────────────────────────────────────
function MetaCard({ label, value, children }) {
  return (
    <div className="bg-white rounded-xl border border-gray-200 px-4 py-3">
      <p className="text-[10px] uppercase tracking-wider text-gray-400 font-semibold mb-1">{label}</p>
      {children ?? <p className="text-sm font-semibold text-gray-800 truncate">{value}</p>}
    </div>
  );
}

function TH({ children, right = false, className = '' }) {
  return (
    <th className={`px-2 py-2 text-gray-500 font-semibold uppercase tracking-wider text-[10px] bg-gray-50 border-b border-gray-200 whitespace-nowrap ${right ? 'text-right' : 'text-left'} ${className}`}>
      {children}
    </th>
  );
}

function NumCell({ val, decimals = 2, className = '', bold = false }) {
  const n = Number(val ?? 0);
  const isZero = n === 0;
  return (
    <td className={`text-right px-2 py-1.5 tabular-nums ${isZero ? 'text-gray-300' : ''} ${bold ? 'font-semibold' : ''} ${className}`}>
      {fmt(val, decimals)}
    </td>
  );
}

function DiffCell({ val }) {
  const n = Number(val ?? 0);
  const color = n > 0 ? 'text-green-600' : n < 0 ? 'text-red-500' : 'text-gray-300';
  return (
    <td className={`text-right px-2 py-1.5 tabular-nums font-semibold ${color}`}>
      {fmt(val)}
    </td>
  );
}

function SectionTitle({ children }) {
  return (
    <div className="flex items-center gap-2 mb-2">
      <div className="flex-1 h-px bg-gray-200" />
      <span className="text-[10px] font-bold uppercase tracking-widest text-gray-400 whitespace-nowrap px-2">{children}</span>
      <div className="flex-1 h-px bg-gray-200" />
    </div>
  );
}

function TableWrap({ children }) {
  return (
    <div className="overflow-x-auto rounded-xl border border-gray-200 bg-white mb-6 shadow-sm">
      {children}
    </div>
  );
}

function SummaryRow({ label, val, bold = false }) {
  return (
    <div className={`flex justify-between items-center py-2 border-b border-gray-100 last:border-0 ${bold ? 'font-bold text-gray-900' : 'text-gray-700'}`}>
      <span className="text-xs">{label}</span>
      <span className="text-xs tabular-nums font-mono">{fmt(val)}</span>
    </div>
  );
}

// ── Cost totals columns ────────────────────────────────────────────────────────
const COST_COLS = [
  { key: 'fobPrice',      label: 'FOB Price' },
  { key: 'inlandFreight', label: 'Inland Freight' },
  { key: 'freight',       label: 'Freight' },
  { key: 'localHandling', label: 'Local Handling' },
  { key: 'duties',        label: 'Duties' },
  { key: 'econSurch',     label: 'Econ. Surch.' },
  { key: 'tax',           label: 'TAX 4.5%' },
  { key: 'insurance',     label: 'Insurance' },
  { key: 'transport',     label: 'Trans. Port' },
  { key: 'unloading',     label: 'Unloading' },
  { key: 'realCost',      label: 'Real Cost' },
];

// ── Item detail columns ────────────────────────────────────────────────────────
const ITEM_COLS = [
  { key: 'uc',          label: 'U/C',         right: true,  dec: 0 },
  { key: 'up',          label: 'U/P',         right: true,  dec: 4 },
  { key: 'ord',         label: 'Ord.',         right: true,  dec: 0 },
  { key: 'fcPrice',     label: 'F/C Price',   right: true,  dec: 4 },
  { key: 'totalInvP',   label: 'Total Inv/P', right: true,  dec: 2 },
  { key: 'fobPrice',    label: 'FOB Price',   right: true,  dec: 2 },
  { key: 'inlandFrt',   label: 'Inland Frt.', right: true,  dec: 2 },
  { key: 'freight',     label: 'Freight',     right: true,  dec: 2 },
  { key: 'localHdl',    label: 'Local Hdl.',  right: true,  dec: 2 },
  { key: 'duties',      label: 'Duties',      right: true,  dec: 2 },
  { key: 'econSrch',    label: 'Econ. Srch.', right: true,  dec: 2 },
  { key: 'tax',         label: 'TAX 4.5%',   right: true,  dec: 2 },
  { key: 'insurance',   label: 'Insurance',   right: true,  dec: 2 },
  { key: 'trans',       label: 'Trans.',       right: true,  dec: 2 },
  { key: 'unload',      label: 'Unload.',     right: true,  dec: 2 },
  { key: 'costPerCase', label: 'Cost p/case', right: true,  dec: 2, bold: true },
  { key: 'totalExt',    label: 'Total Ext.',  right: true,  dec: 2, bold: true },
];

// ── Main component ─────────────────────────────────────────────────────────────
export default function FinalCostReport({
  reportNumber  = '868',
  factor        = 1.82,
  order         = 'ORD-2026-001',
  warehouse     = 'Main Warehouse',
  warehouseCode = '11060',
  container     = 'TCKU3456789',
  invoiceNo     = 'INV-2026-100',
  invoiceDate   = '2026-05-22',
  invRate       = 1.82,
  forwarder     = 'DHL Global Forwarding',
  vendor        = 'Diageo International Ltd.',
  costTotals    = SAMPLE_COST_TOTALS,
  items         = SAMPLE_ITEMS,
  summaries     = SAMPLE_SUMMARIES,
  reportDate    = new Date().toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: '2-digit' }),
  page          = '1 of 1',
}) {
  const totCost = (key) => costTotals.reduce((s, r) => s + (Number(r[key]) || 0), 0);
  const totItem = (key) => items.reduce((s, r) => s + (Number(r[key]) || 0), 0);

  return (
    <div className="min-h-screen bg-gray-50 p-6 font-sans text-gray-800 text-sm print:p-0 print:bg-white">

      {/* ── 1. Header bar ─────────────────────────────────────────────────── */}
      <div className="flex items-center justify-between bg-white rounded-xl border border-gray-200 px-6 py-4 mb-4 shadow-sm">
        <div>
          <h1 className="text-xl font-bold text-gray-900 tracking-tight leading-tight">Final Cost Calculation</h1>
          <p className="text-xs text-gray-400 mt-0.5">Import Cost Report — XCG</p>
        </div>
        <span className="bg-blue-500 text-white text-base font-bold px-5 py-1.5 rounded-full shadow">
          #{reportNumber}
        </span>
      </div>

      {/* ── 2. Metadata row 1 ─────────────────────────────────────────────── */}
      <div className="grid grid-cols-4 gap-3 mb-3">
        <MetaCard label="Container" value={container || '—'} />
        <MetaCard label="Order" value={order} />
        <MetaCard label="Warehouse" value={`${warehouse} (${warehouseCode})`} />
        <MetaCard label="Factor">
          <span className="inline-block mt-1 bg-amber-100 text-amber-700 font-bold text-sm px-3 py-0.5 rounded-full border border-amber-200">
            {factor}
          </span>
        </MetaCard>
      </div>

      {/* ── 3. Metadata row 2 ─────────────────────────────────────────────── */}
      <div className="grid grid-cols-3 gap-3 mb-6">
        <MetaCard label="Invoice No." value={invoiceNo} />
        <MetaCard label="Invoice Date / Stat. Inv. Rate">
          <p className="text-sm font-semibold text-gray-800 mt-0.5">
            {invoiceDate}
            <span className="mx-2 text-gray-300">·</span>
            <span className="text-gray-600 font-normal">{fmt(invRate, 4)}</span>
          </p>
        </MetaCard>
        <MetaCard label="Forwarder / Vendor">
          <p className="text-sm font-semibold text-gray-800 mt-0.5 truncate">{forwarder}</p>
          <p className="text-xs text-gray-400 truncate">{vendor}</p>
        </MetaCard>
      </div>

      {/* ── 4. Cost Components Totals ──────────────────────────────────────── */}
      <SectionTitle>Cost Components Totals in XCG</SectionTitle>
      <TableWrap>
        <table className="w-full text-[11px]">
          <thead>
            <tr>
              <TH>#</TH>
              {COST_COLS.map(c => <TH key={c.key} right>{c.label}</TH>)}
            </tr>
          </thead>
          <tbody>
            {costTotals.map((r, i) => (
              <tr key={i} className={i % 2 === 0 ? 'bg-white' : 'bg-gray-50/50'}>
                <td className="px-2 py-1.5 font-medium text-gray-700 whitespace-nowrap">{r.poNo}</td>
                {COST_COLS.map(c => (
                  <NumCell key={c.key} val={r[c.key]} bold={c.key === 'realCost'} />
                ))}
              </tr>
            ))}
          </tbody>
          <tfoot>
            <tr className="bg-gray-100 border-t-2 border-gray-300 text-[11px] font-bold">
              <td className="px-2 py-2 uppercase text-gray-500 tracking-wider">Totals</td>
              {COST_COLS.map(c => (
                <td key={c.key} className="text-right px-2 py-2 tabular-nums">{fmt(totCost(c.key))}</td>
              ))}
            </tr>
          </tfoot>
        </table>
      </TableWrap>

      {/* ── 5. Item Detail Table ───────────────────────────────────────────── */}
      <SectionTitle>Item Detail — Cost per Case in XCG</SectionTitle>
      <TableWrap>
        <table className="w-full text-[10px]">
          <thead>
            <tr>
              <TH>Item No.</TH>
              <TH>Description</TH>
              {ITEM_COLS.map(c => <TH key={c.key} right>{c.label}</TH>)}
              <TH right>Diff. Cost</TH>
            </tr>
          </thead>
          <tbody>
            {items.map((d, i) => (
              <tr key={i} className={i % 2 === 0 ? 'bg-white' : 'bg-gray-50/50'}>
                <td className="px-2 py-1 font-mono text-gray-700 whitespace-nowrap">{d.itemNo}</td>
                <td className="px-2 py-1 text-gray-700 max-w-[160px] truncate" title={d.description}>{d.description}</td>
                {ITEM_COLS.map(c => (
                  <NumCell key={c.key} val={d[c.key]} decimals={c.dec ?? 2} bold={c.bold} />
                ))}
                <DiffCell val={d.diffCost} />
              </tr>
            ))}
          </tbody>
          <tfoot>
            {/* Totals row */}
            <tr className="bg-gray-100 border-t-2 border-gray-300 text-[10px] font-bold">
              <td className="px-2 py-2 uppercase text-gray-500 tracking-wider" colSpan={2}>Totals</td>
              <td className="text-right px-2 py-2 tabular-nums">{fmt(totItem('uc'), 0)}</td>
              <td className="px-2 py-2" />
              <td className="text-right px-2 py-2 tabular-nums">{fmt(totItem('ord'), 0)}</td>
              <td className="px-2 py-2" />
              {['totalInvP','fobPrice','inlandFrt','freight','localHdl','duties','econSrch','tax','insurance','trans','unload','costPerCase','totalExt'].map(k => (
                <td key={k} className="text-right px-2 py-2 tabular-nums">{fmt(totItem(k))}</td>
              ))}
              <td className={`text-right px-2 py-2 tabular-nums ${totItem('diffCost') > 0 ? 'text-green-600' : totItem('diffCost') < 0 ? 'text-red-500' : 'text-gray-300'}`}>
                {fmt(totItem('diffCost'))}
              </td>
            </tr>
            {/* Invoice Total row */}
            <tr className="bg-blue-50 border-t border-blue-100 text-[10px]">
              <td className="px-2 py-2 uppercase text-blue-700 font-bold tracking-wider" colSpan={2}>Invoice Total</td>
              <td className="px-2 py-2" />
              <td className="px-2 py-2" />
              <td className="text-right px-2 py-2 tabular-nums font-bold text-blue-700">{fmt(totItem('ord'), 0)}</td>
              <td className="px-2 py-2" />
              <td className="text-right px-2 py-2 tabular-nums font-bold text-blue-700">{fmt(totItem('totalInvP'))}</td>
              <td colSpan={13} />
            </tr>
          </tfoot>
        </table>
      </TableWrap>

      {/* ── 6. Summary grid ───────────────────────────────────────────────── */}
      {summaries.length > 0 && (
        <>
          <SectionTitle>Summary</SectionTitle>
          <div className={`grid gap-4 mb-6 ${summaries.length === 2 ? 'grid-cols-2' : 'grid-cols-1 max-w-sm'}`}>
            {summaries.map((s, i) => (
              <div key={i} className="bg-white rounded-xl border border-gray-200 p-5 shadow-sm">
                <p className="text-xs font-bold uppercase tracking-widest text-gray-400 mb-4 pb-2 border-b border-gray-100">
                  {s.label}
                </p>
                <SummaryRow label="Total Extended"   val={s.totalExtended} />
                <SummaryRow label="Total Actual VIP" val={s.totalActualVip} />
                <SummaryRow label="Total Real Cost"  val={s.totalRealCost} bold />
              </div>
            ))}
          </div>
        </>
      )}

      {/* ── 7. Footer ─────────────────────────────────────────────────────── */}
      <div className="flex justify-between items-center text-[10px] text-gray-400 pt-3 border-t border-gray-200 mt-2">
        <span>{reportDate}</span>
        <span>Page {page}</span>
      </div>
    </div>
  );
}

// ── Sample data ────────────────────────────────────────────────────────────────
const SAMPLE_COST_TOTALS = [
  {
    poNo: '1007-26-CMB005',
    fobPrice: 42500.00, inlandFreight: 850.00, freight: 7871.50,
    localHandling: 1200.00, duties: 3800.00, econSurch: 420.00,
    tax: 1912.50, insurance: 318.75, transport: 650.00,
    unloading: 280.00, realCost: 59802.75,
  },
  {
    poNo: '1007-26-CMB006',
    fobPrice: 18200.00, inlandFreight: 364.00, freight: 3378.50,
    localHandling: 514.80, duties: 1638.00, econSurch: 180.00,
    tax: 819.00, insurance: 136.50, transport: 278.85,
    unloading: 120.00, realCost: 25629.65,
  },
];

const SAMPLE_ITEMS = [
  {
    itemNo: '101001', description: 'JOHNNIE WALKER BLACK 750ML',
    uc: 12, up: 28.5000, ord: 50, fcPrice: 15.6593, totalInvP: 9395.58,
    fobPrice: 17118.00, inlandFreight: 342.36, freight: 3172.20,
    localHdl: 493.20, duties: 1540.62, econSrch: 170.07, tax: 765.31,
    insurance: 128.39, trans: 265.23, unload: 114.36,
    costPerCase: 478.21, totalExt: 23910.50, diffCost: 124.30,
  },
  {
    itemNo: '101002', description: 'JOHNNIE WALKER RED 750ML',
    uc: 12, up: 18.2000, ord: 80, fcPrice: 9.9945, totalInvP: 9594.72,
    fobPrice: 17462.40, inlandFreight: 349.25, freight: 3236.50,
    localHdl: 503.20, duties: 1572.00, econSrch: 173.40, tax: 780.48,
    insurance: 131.01, trans: 270.72, unload: 116.70,
    costPerCase: 297.22, totalExt: 23777.60, diffCost: -18.50,
  },
  {
    itemNo: '201001', description: 'CORONA EXTRA 330ML 24PK',
    uc: 24, up: 12.5000, ord: 100, fcPrice: 6.8681, totalInvP: 16483.44,
    fobPrice: 30000.00, inlandFreight: 600.00, freight: 5563.80,
    localHdl: 864.60, duties: 2700.00, econSrch: 297.90, tax: 1340.55,
    insurance: 225.00, trans: 464.85, unload: 200.40,
    costPerCase: 422.57, totalExt: 42257.00, diffCost: 0,
  },
];

const SAMPLE_SUMMARIES = [
  {
    label: '11060',
    totalExtended: 52344.20, totalActualVip: 49800.00, totalRealCost: 59802.75,
  },
  {
    label: '11060 / 11020',
    totalExtended: 89988.10, totalActualVip: 85200.00, totalRealCost: 85432.40,
  },
];
