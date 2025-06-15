'use client';

import { useEffect, useState } from 'react';
import { useAuthStore } from '../store/auth';
import { useJokesStore } from '../store/jokes';
import { Joke } from '../types';

// Helper function to get grid icons
const getGridIcon = (columns: number): string => {
  switch (columns) {
    case 1:
      return 'M3 3h18v2H3V3zm0 8h18v2H3v-2zm0 8h18v2H3v-2z';
    case 2:
      return 'M3 3h8v8H3V3zm10 0h8v8h-8V3zM3 13h8v8H3v-8zm10 0h8v8h-8v-8z';
    case 3:
      return 'M3 3h5v5H3V3zm7 0h5v5h-5V3zm7 0h5v5h-5V3zM3 10h5v5H3v-5zm7 0h5v5h-5v-5zm7 0h5v5h-5v-5z';
    case 4:
      return 'M2 2h4v4H2V2zm6 0h4v4H8V2zm6 0h4v4h-4V2zm6 0h4v4h-4V2zM2 8h4v4H2V8zm6 0h4v4H8V8zm6 0h4v4h-4V8zm6 0h4v4h-4V8z';
    default:
      return 'M3 3h5v5H3V3zm7 0h5v5h-5V3zm7 0h5v5h-5V3zM3 10h5v5H3v-5zm7 0h5v5h-5v-5zm7 0h5v5h-5v-5z';
  }
};

// Helper function to get grid CSS classes
const getGridClasses = (columns: number): string => {
  switch (columns) {
    case 1:
      return 'grid-cols-1';
    case 2:
      return 'grid-cols-1 md:grid-cols-2';
    case 3:
      return 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3';
    case 4:
      return 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4';
    default:
      return 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3';
  }
};

const JokesPage = () => {
  const [searchQuery, setSearchQuery] = useState('');
  const [gridColumns, setGridColumns] = useState(3);
  const { user, logout, token } = useAuthStore();
  const { 
    jokes, 
    isLoading, 
    error, 
    pagination, 
    fetchJokes, 
    searchJokes, 
    clearError,
    setPage 
  } = useJokesStore();

  useEffect(() => {
    if (token) {
      fetchJokes(token);
    }
  }, [token, fetchJokes]);

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!token) return;
    
    if (searchQuery.trim()) {
      await searchJokes(token, searchQuery);
    } else {
      await fetchJokes(token);
    }
  };

  const handlePageChange = async (newPage: number) => {
    if (!token) return;
    setPage(newPage);
    await fetchJokes(token, newPage, pagination.pageSize);
  };

  const handleLogout = async () => {
    await logout();
  };

  if (error) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
        <div className="bg-white rounded-lg shadow-md p-6 max-w-md w-full text-center">
          <div className="text-red-500 mb-4">
            <svg className="w-12 h-12 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
          </div>
          <h2 className="text-xl font-semibold text-gray-900 mb-2">Something went wrong</h2>
          <p className="text-gray-600 mb-4">{error}</p>
          <button
            onClick={clearError}
            className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 transition-colors"
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-indigo-50 via-white to-purple-50">
      {/* Header */}
      <header className="bg-white/80 backdrop-blur-md shadow-lg border-b border-white/20 sticky top-0 z-50">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-20">
            <div className="flex items-center gap-6">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 bg-gradient-to-r from-blue-500 to-purple-600 rounded-xl flex items-center justify-center">
                  <span className="text-white font-bold text-xl">😂</span>
                </div>
                <div>
                  <h1 className="text-2xl font-bold bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent">
                    JokeAPI
                  </h1>
                  <p className="text-sm text-gray-500">Discover amazing jokes</p>
                </div>
              </div>
              <div className="hidden md:flex items-center gap-2 bg-blue-50 px-3 py-1.5 rounded-full">
                <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse"></div>
                <span className="text-sm text-blue-700 font-medium">
                  Welcome, {user?.name}!
                </span>
              </div>
            </div>
            <div className="flex items-center gap-4">
              <div className="hidden sm:flex items-center gap-2 text-sm text-gray-500">
                <span className="flex items-center gap-1">
                  <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
                    <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
                  </svg>
                  {jokes.length} jokes
                </span>
              </div>
              <button
                onClick={handleLogout}
                className="bg-gradient-to-r from-red-500 to-pink-500 hover:from-red-600 hover:to-pink-600 text-white px-6 py-2.5 rounded-xl font-medium transition-all duration-200 transform hover:scale-105 shadow-md hover:shadow-lg"
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Hero Section */}
        <div className="text-center mb-12">
          <h2 className="text-4xl font-bold text-gray-900 mb-4">
            Ready to <span className="text-transparent bg-clip-text bg-gradient-to-r from-yellow-400 to-orange-500">laugh</span>?
          </h2>
          <p className="text-xl text-gray-600 max-w-2xl mx-auto">
            Discover the funniest jokes from our community or share your own!
          </p>
        </div>

        {/* Search Bar */}
        <div className="mb-12">
          <form onSubmit={handleSearch} className="max-w-2xl mx-auto">
            <div className="relative">
              <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
                <svg className="h-5 w-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                </svg>
              </div>
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search for the perfect joke..."
                className="w-full pl-12 pr-32 py-4 text-lg border-2 border-gray-200 rounded-2xl focus:outline-none focus:ring-4 focus:ring-blue-500/20 focus:border-blue-500 transition-all duration-200 bg-white/80 backdrop-blur-sm shadow-lg"
              />
              <div className="absolute inset-y-0 right-0 flex items-center gap-2 pr-2">
                {searchQuery && (
                  <button
                    type="button"
                    onClick={() => {
                      setSearchQuery('');
                      if (token) fetchJokes(token);
                    }}
                    className="bg-gray-100 hover:bg-gray-200 text-gray-600 px-3 py-2 rounded-xl transition-colors text-sm font-medium"
                  >
                    Clear
                  </button>
                )}
                <button
                  type="submit"
                  disabled={isLoading}
                  className="bg-gradient-to-r from-blue-500 to-purple-600 hover:from-blue-600 hover:to-purple-700 text-white px-6 py-2 rounded-xl font-medium transition-all duration-200 transform hover:scale-105 shadow-md hover:shadow-lg disabled:opacity-50 disabled:cursor-not-allowed disabled:transform-none"
                >
                  {isLoading ? (
                    <div className="flex items-center gap-2">
                      <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></div>
                      Searching...
                    </div>
                  ) : (
                    'Search'
                  )}
                </button>
              </div>
            </div>
          </form>
        </div>

        {/* Grid Layout Controls */}
        <div className="flex items-center justify-between mb-8">
          <div className="flex items-center gap-4">
            <span className="text-sm font-medium text-gray-700">Layout:</span>
            <div className="flex items-center gap-2 bg-white/80 backdrop-blur-sm rounded-xl p-1 shadow-sm border border-gray-200">
              {[1, 2, 3, 4].map((columns) => (
                <button
                  key={columns}
                  onClick={() => setGridColumns(columns)}
                  className={`px-3 py-2 rounded-lg text-sm font-medium transition-all duration-200 ${
                    gridColumns === columns
                      ? 'bg-gradient-to-r from-blue-500 to-purple-600 text-white shadow-md'
                      : 'text-gray-600 hover:text-blue-600 hover:bg-blue-50'
                  }`}
                  title={`${columns} column${columns > 1 ? 's' : ''}`}
                >
                  <div className="flex items-center gap-1">
                    <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
                      <path d={getGridIcon(columns)} />
                    </svg>
                    {columns}
                  </div>
                </button>
              ))}
            </div>
          </div>
          
          {jokes.length > 0 && (
            <div className="text-sm text-gray-500">
              Showing {jokes.length} joke{jokes.length !== 1 ? 's' : ''}
            </div>
          )}
        </div>

        {/* Loading State */}
        {isLoading && (
          <div className="flex items-center justify-center py-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
          </div>
        )}

        {/* Jokes Grid */}
        {!isLoading && (
          <>
            <div className={`grid gap-6 mb-8 ${getGridClasses(gridColumns)}`}>
              {jokes.map((joke) => (
                <JokeCard key={joke.id} joke={joke} />
              ))}
            </div>

            {jokes.length === 0 && !isLoading && (
              <div className="text-center py-12">
                <div className="text-gray-400 mb-4">
                  <svg className="w-16 h-16 mx-auto" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1} d="M9.172 16.172a4 4 0 015.656 0M9 12h6m-6-4h6m2 5.291A7.962 7.962 0 0112 15c-2.034 0-3.9.785-5.291 2.063M6.343 6.343A8 8 0 1018.364 18.364L6.343 6.343z" />
                  </svg>
                </div>
                <h3 className="text-lg font-medium text-gray-900 mb-2">No jokes found</h3>
                <p className="text-gray-500">
                  {searchQuery ? 'Try a different search term' : 'No jokes available at the moment'}
                </p>
              </div>
            )}

            {/* Pagination */}
            {pagination.totalPages > 1 && (
              <div className="flex items-center justify-center gap-2">
                <button
                  onClick={() => handlePageChange(pagination.page - 1)}
                  disabled={pagination.page <= 1 || isLoading}
                  className="px-3 py-2 text-sm font-medium text-gray-500 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Previous
                </button>
                
                <span className="px-3 py-2 text-sm text-gray-700">
                  Page {pagination.page} of {pagination.totalPages}
                </span>
                
                <button
                  onClick={() => handlePageChange(pagination.page + 1)}
                  disabled={pagination.page >= pagination.totalPages || isLoading}
                  className="px-3 py-2 text-sm font-medium text-gray-500 bg-white border border-gray-300 rounded-md hover:bg-gray-50 disabled:opacity-50 disabled:cursor-not-allowed"
                >
                  Next
                </button>
              </div>
            )}
          </>
        )}
      </main>
    </div>
  );
};

const JokeCard = ({ joke }: { joke: Joke }) => {
  const [isLiked, setIsLiked] = useState(false);
  
  return (
    <div className="group bg-white/80 backdrop-blur-sm rounded-2xl shadow-lg hover:shadow-2xl transition-all duration-300 p-6 border border-white/20 hover:border-blue-200/50 transform hover:-translate-y-1">
      {/* Joke Type Badge */}
      <div className="flex items-center justify-between mb-4">
        <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-semibold bg-gradient-to-r from-blue-100 to-purple-100 text-blue-800 border border-blue-200/50">
          {joke.type}
        </span>
        <div className="flex items-center gap-2">
          <button
            onClick={() => setIsLiked(!isLiked)}
            title={isLiked ? 'Unlike joke' : 'Like joke'}
            aria-label={isLiked ? 'Unlike joke' : 'Like joke'}
            className={`p-2 rounded-full transition-all duration-200 ${
              isLiked 
                ? 'bg-red-100 text-red-500 scale-110' 
                : 'text-gray-400 hover:bg-red-50 hover:text-red-400'
            }`}
          >
            <svg className="w-5 h-5" fill={isLiked ? 'currentColor' : 'none'} stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4.318 6.318a4.5 4.5 0 000 6.364L12 20.364l7.682-7.682a4.5 4.5 0 00-6.364-6.364L12 7.636l-1.318-1.318a4.5 4.5 0 00-6.364 0z" />
            </svg>
          </button>
          <span className="flex items-center gap-1 text-sm font-medium text-gray-600">
            {joke.laughScore + (isLiked ? 1 : 0)}
          </span>
        </div>
      </div>

      {/* Joke Content */}
      <div className="mb-6">
        <h3 className="text-xl font-bold text-gray-900 mb-3 group-hover:text-blue-600 transition-colors">
          {joke.title}
        </h3>
        <div className="bg-gradient-to-br from-yellow-50 to-orange-50 rounded-xl p-4 border-l-4 border-yellow-400">
          <p className="text-gray-700 leading-relaxed text-lg font-medium italic">
            &ldquo;{joke.content}&rdquo;
          </p>
        </div>
      </div>
      
      {/* Tags */}
      {joke.tags.length > 0 && (
        <div className="flex flex-wrap gap-2 mb-4">
          {joke.tags.slice(0, 3).map((tag) => (
            <span
              key={tag.id}
              className="inline-flex items-center px-2.5 py-1 rounded-lg text-xs font-medium bg-gray-100 text-gray-700 hover:bg-blue-100 hover:text-blue-700 transition-colors cursor-pointer"
            >
              #{tag.name}
            </span>
          ))}
          {joke.tags.length > 3 && (
            <span className="inline-flex items-center px-2.5 py-1 rounded-lg text-xs font-medium text-gray-500 bg-gray-50">
              +{joke.tags.length - 3} more
            </span>
          )}
        </div>
      )}
      
      {/* Footer */}
      <div className="flex items-center justify-between pt-4 border-t border-gray-100">
        <div className="flex items-center gap-3 text-sm text-gray-500">
          <span className="flex items-center gap-1">
            <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 24 24">
              <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/>
            </svg>
            {new Date(joke.createdAt).toLocaleDateString()}
          </span>
        </div>
        <div className="flex items-center gap-2">
          <button title="Share joke" aria-label="Share joke" className="p-2 text-gray-400 hover:text-blue-500 hover:bg-blue-50 rounded-lg transition-colors">
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8.684 13.342C8.886 12.938 9 12.482 9 12c0-.482-.114-.938-.316-1.342m0 2.684a3 3 0 110-2.684m0 2.684l6.632 3.316m-6.632-6l6.632-3.316m0 0a3 3 0 105.367-2.684 3 3 0 00-5.367 2.684zm0 9.316a3 3 0 105.367 2.684 3 3 0 00-5.367-2.684z" />
            </svg>
          </button>
          <button title="Save joke" aria-label="Save joke" className="p-2 text-gray-400 hover:text-green-500 hover:bg-green-50 rounded-lg transition-colors">
            <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 5a2 2 0 012-2h10a2 2 0 012 2v16l-7-3.5L5 21V5z" />
            </svg>
          </button>
        </div>
      </div>
    </div>
  );
};

export default JokesPage; 