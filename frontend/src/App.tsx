function App() {
  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-green-600 text-white shadow-lg">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-6">
            <div className="flex items-center">
              <h1 className="text-3xl font-bold">GAAStat</h1>
              <p className="ml-4 text-green-200">GAA Statistics & Analytics</p>
            </div>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto py-12 px-4 sm:px-6 lg:px-8">
        <div className="text-center">
          <h2 className="text-4xl font-bold text-gray-900 mb-4">
            Welcome to GAAStat
          </h2>
          <p className="text-xl text-gray-600 mb-8">
            Your comprehensive platform for GAA team and player statistics
          </p>
          
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8 mt-12">
            <div className="bg-white rounded-lg shadow-md p-6">
              <div className="text-2xl text-green-600 mb-4">🏑</div>
              <h3 className="text-xl font-semibold text-gray-900 mb-2">Teams</h3>
              <p className="text-gray-600">Manage your hurling and football teams</p>
            </div>
            
            <div className="bg-white rounded-lg shadow-md p-6">
              <div className="text-2xl text-blue-600 mb-4">👤</div>
              <h3 className="text-xl font-semibold text-gray-900 mb-2">Players</h3>
              <p className="text-gray-600">Track player profiles and statistics</p>
            </div>
            
            <div className="bg-white rounded-lg shadow-md p-6">
              <div className="text-2xl text-orange-600 mb-4">📊</div>
              <h3 className="text-xl font-semibold text-gray-900 mb-2">Matches</h3>
              <p className="text-gray-600">Record and analyze match performances</p>
            </div>
          </div>
        </div>
      </main>
    </div>
  )
}

export default App