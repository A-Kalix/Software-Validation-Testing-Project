const Topbar = () => (
  <div className="flex justify-between items-center">
    <div>
      <h1 className="text-2xl font-bold text-slate-800">Academic Dashboard</h1>
      <p className="text-slate-500 text-sm">Welcome back, Kübra</p>
    </div>
    <div className="flex items-center gap-4">
      <div className="text-right hidden md:block">
        <p className="text-sm font-bold text-slate-700">Kübra Laçin</p>
        <p className="text-xs text-slate-500">Software Engineer</p>
      </div>
      <div className="w-10 h-10 bg-blue-600 rounded-full flex items-center justify-center text-white font-bold shadow-lg">K</div>
    </div>
  </div>
);

export default Topbar;