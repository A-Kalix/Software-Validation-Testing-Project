import { LayoutDashboard, GraduationCap, BookOpen, Users, Building2 } from "lucide-react";

const NavItem = ({ icon: Icon, label, active = false }) => (
  <button className={`w-full flex items-center gap-3 px-4 py-3 rounded-xl transition-all ${
    active ? "bg-blue-600 text-white shadow-lg" : "text-slate-400 hover:bg-slate-800 hover:text-white"
  }`}>
    <Icon size={20} />
    <span className="font-medium text-sm">{label}</span>
  </button>
);

const Sidebar = () => (
  <aside className="w-64 h-screen bg-slate-900 fixed left-0 top-0 p-6 border-r border-slate-800">
    <div className="flex items-center gap-3 px-2 mb-10 text-white font-bold text-xl">
      <div className="p-2 bg-blue-500 rounded-lg"><GraduationCap /></div>
      <span>UniPortal</span>
    </div>
    <nav className="space-y-2">
      <NavItem icon={LayoutDashboard} label="Dashboard" active />
      <NavItem icon={Users} label="Users" />
      <NavItem icon={BookOpen} label="Courses" />
      <NavItem icon={GraduationCap} label="Sections" />
      <NavItem icon={Building2} label="Departments" />
    </nav>
  </aside>
);

export default Sidebar;