const CourseTable = ({ courses, isLoading }) => (
  <div className="bg-white rounded-2xl shadow-sm border border-slate-100 overflow-hidden">
    <div className="p-6 border-b border-slate-50 flex justify-between items-center">
      <h3 className="font-bold text-slate-800">Recent Courses</h3>
      <button className="text-blue-600 text-sm font-semibold hover:underline">View All</button>
    </div>
    <table className="w-full text-left">
      <thead className="bg-slate-50 text-slate-500 text-xs uppercase">
        <tr>
          <th className="px-6 py-4">Code</th>
          <th className="px-6 py-4">Title</th>
          <th className="px-6 py-4 text-center">Credits</th>
        </tr>
      </thead>
      <tbody className="divide-y divide-slate-100">
        {isLoading ? (
          <tr><td colSpan="3" className="p-10 text-center text-slate-400">Fetching data...</td></tr>
        ) : (
          courses.map((c) => (
            <tr key={c.id} className="hover:bg-slate-50 transition-colors">
              <td className="px-6 py-4 font-mono font-bold text-blue-600">{c.courseCode}</td>
              <td className="px-6 py-4 text-slate-700">{c.title}</td>
              <td className="px-6 py-4 text-center text-slate-600">{c.credits}</td>
            </tr>
          ))
        )}
      </tbody>
    </table>
  </div>
);

export default CourseTable;