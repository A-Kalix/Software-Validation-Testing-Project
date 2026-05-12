const StatCard = ({ title, value, trend }) => (
  <div className="bg-white p-6 rounded-2xl shadow-sm border border-slate-100 transition-transform hover:scale-[1.02]">
    <p className="text-slate-500 text-xs font-semibold uppercase tracking-wider mb-1">{title}</p>
    <div className="flex items-end justify-between">
      <h3 className="text-3xl font-bold text-slate-800">{value}</h3>
      <span className="text-emerald-500 text-xs font-bold bg-emerald-50 px-2 py-1 rounded-lg">{trend}</span>
    </div>
  </div>
);

export default StatCard;