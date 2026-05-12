import { useEffect, useState } from "react";
import api from "../api/axios";
import Sidebar from "../components/Sidebar";
import Topbar from "../components/Topbar";
import StatCard from "../components/StatCard";
import CourseTable from "../components/CourseTable";

const Dashboard = () => {
  const [data, setData] = useState({ stats: {}, courses: [], loading: true });

  useEffect(() => {
    const loadData = async () => {
      try {
        const [u, c, s, d] = await Promise.all([
          api.get("/users"), api.get("/courses"),
          api.get("/sections"), api.get("/departments")
        ]);
        setData({
          stats: { students: u.data.length, courses: c.data.length, sections: s.data.length, departments: d.data.length },
          courses: c.data.slice(0, 5),
          loading: false
        });
      } catch (err) {
        console.error("Fetch error:", err);
        setData(prev => ({ ...prev, loading: false }));
      }
    };
    loadData();
  }, []);

  return (
    <div className="flex min-h-screen bg-[#f8fafc]">
      <Sidebar />
      <main className="flex-1 ml-64 p-8">
        <Topbar />
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mt-8">
          <StatCard title="Total Students" value={data.stats.students || 0} trend="+5%" />
          <StatCard title="Active Courses" value={data.stats.courses || 0} trend="Live" />
          <StatCard title="Total Sections" value={data.stats.sections || 0} trend="Open" />
          <StatCard title="Departments" value={data.stats.departments || 0} trend="Full" />
        </div>
        <div className="mt-8">
          <CourseTable courses={data.courses} isLoading={data.loading} />
        </div>
      </main>
    </div>
  );
};

export default Dashboard;