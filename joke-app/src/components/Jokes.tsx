import React, { useState, useEffect } from 'react';
import { useAuthStore } from '../store/authStore';
import { useNavigate } from 'react-router-dom';
import { apiService } from '../services/api';
import { Joke, JokesResponse, JokeSearchParams } from '../types/auth';
import { CreateJokeModal } from './CreateJokeModal';
import { 
  User, 
  Shield, 
  LogOut, 
  Search, 
  Plus,
  Laugh,
  Calendar,
  Tag as TagIcon,
  RefreshCw,
  ChevronLeft,
  ChevronRight,
  Loader2
} from 'lucide-react';

export const Jokes: React.FC = () => {
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();
  
  const [jokes, setJokes] = useState<Joke[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalJokes, setTotalJokes] = useState(0);
  const [sortBy, setSortBy] = useState<'laughScore' | 'createdAt' | 'modifiedAt'>('createdAt');
  const [sortOrder, setSortOrder] = useState<'asc' | 'desc'>('desc');
  const [showCreateForm, setShowCreateForm] = useState(false);

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  const getRoleIcon = (role: string) => {
    switch (role) {
      case 'admin':
        return <Shield className="w-5 h-5 text-yellow-500" />;
      case 'moderator':
        return <Shield className="w-5 h-5 text-blue-500" />;
      default:
        return <User className="w-5 h-5 text-gray-500" />;
    }
  };

  const loadJokes = async (params?: JokeSearchParams) => {
    try {
      setLoading(true);
      setError(null);
      
      const searchParams: JokeSearchParams = {
        page: currentPage,
        limit: 10,
        sortBy,
        sortOrder,
        isActive: true,
        ...params
      };

      if (searchQuery.trim()) {
        searchParams.query = searchQuery.trim();
      }

      const response: JokesResponse = await apiService.getJokes(searchParams);
      
      setJokes(response.jokes || []);
      setTotalPages(response.totalPages || 1);
      setTotalJokes(response.total || 0);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load jokes');
      setJokes([]);
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setCurrentPage(1);
    loadJokes();
  };

  const handlePageChange = (newPage: number) => {
    setCurrentPage(newPage);
  };

  const handleSortChange = (newSortBy: typeof sortBy) => {
    if (sortBy === newSortBy) {
      setSortOrder(sortOrder === 'desc' ? 'asc' : 'desc');
    } else {
      setSortBy(newSortBy);
      setSortOrder('desc');
    }
    setCurrentPage(1);
  };

  const formatDate = (dateString: string) => {
    // Handle invalid dates from the API
    if (dateString === "0001-01-01T00:00:00") {
      return "Unknown date";
    }
    
    return new Date(dateString).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  const handleJokeCreated = () => {
    // Refresh the jokes list after a new joke is created
    setCurrentPage(1);
    loadJokes();
  };

  useEffect(() => {
    loadJokes();
  }, [currentPage, sortBy, sortOrder]);

  if (!user) {
    return null;
  }

  return (
    <>
      <CreateJokeModal
        isOpen={showCreateForm}
        onClose={() => setShowCreateForm(false)}
        onJokeCreated={handleJokeCreated}
      />
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50">
      {/* Header */}
      <header className="bg-white/70 backdrop-blur-sm border-b border-white/20 sticky top-0 z-10">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <div className="flex items-center space-x-3">
              <div className="bg-gradient-to-r from-blue-600 to-purple-600 w-10 h-10 rounded-lg flex items-center justify-center">
                <Laugh className="w-6 h-6 text-white" />
              </div>
              <h1 className="text-xl font-bold text-gray-900">WhateverAPI Jokes</h1>
            </div>
            
            <div className="flex items-center space-x-4">
              <div className="flex items-center space-x-2">
                {getRoleIcon(user.role)}
                <span className="text-sm font-medium text-gray-700">{user.name}</span>
              </div>
              
              <button
                onClick={handleLogout}
                className="bg-red-50 hover:bg-red-100 text-red-600 p-2 rounded-lg transition-colors"
                title="Logout"
              >
                <LogOut className="w-5 h-5" />
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* Search and Controls */}
        <div className="bg-white/70 backdrop-blur-sm rounded-2xl p-6 shadow-xl border border-white/20 mb-8">
          <div className="flex flex-col lg:flex-row gap-4 items-center justify-between">
            {/* Search Form */}
            <form onSubmit={handleSearch} className="flex-1 flex gap-3">
              <div className="relative flex-1">
                <Search className="w-5 h-5 text-gray-400 absolute left-3 top-1/2 transform -translate-y-1/2" />
                <input
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  placeholder="Search jokes..."
                  className="w-full pl-10 pr-4 py-2 border border-gray-200 rounded-xl focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all bg-white/50 backdrop-blur-sm"
                />
              </div>
              <button
                type="submit"
                className="bg-blue-600 hover:bg-blue-700 text-white px-6 py-2 rounded-xl font-medium transition-colors flex items-center space-x-2"
              >
                <Search className="w-4 h-4" />
                <span>Search</span>
              </button>
            </form>

            {/* Sort Controls */}
            <div className="flex gap-2">
              <button
                onClick={() => handleSortChange('createdAt')}
                className={`px-4 py-2 rounded-lg font-medium transition-colors flex items-center space-x-2 ${
                  sortBy === 'createdAt' 
                    ? 'bg-blue-100 text-blue-700' 
                    : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                }`}
              >
                <Calendar className="w-4 h-4" />
                <span>Date {sortBy === 'createdAt' && (sortOrder === 'desc' ? '↓' : '↑')}</span>
              </button>
              
              <button
                onClick={() => handleSortChange('laughScore')}
                className={`px-4 py-2 rounded-lg font-medium transition-colors flex items-center space-x-2 ${
                  sortBy === 'laughScore' 
                    ? 'bg-blue-100 text-blue-700' 
                    : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
                }`}
              >
                <Laugh className="w-4 h-4" />
                <span>Score {sortBy === 'laughScore' && (sortOrder === 'desc' ? '↓' : '↑')}</span>
              </button>

              <button
                onClick={() => loadJokes()}
                className="bg-green-100 hover:bg-green-200 text-green-700 px-4 py-2 rounded-lg font-medium transition-colors flex items-center space-x-2"
              >
                <RefreshCw className="w-4 h-4" />
                <span>Refresh</span>
              </button>

              <button
                onClick={() => setShowCreateForm(true)}
                className="bg-purple-100 hover:bg-purple-200 text-purple-700 px-4 py-2 rounded-lg font-medium transition-colors flex items-center space-x-2"
              >
                <Plus className="w-4 h-4" />
                <span>New Joke</span>
              </button>
            </div>
          </div>

          {/* Stats */}
          <div className="mt-4 flex justify-between items-center text-sm text-gray-600">
            <span>Showing {jokes.length} of {totalJokes} jokes</span>
            <span>Page {currentPage} of {totalPages}</span>
          </div>
        </div>

        {/* Error State */}
        {error && (
          <div className="bg-red-50 border border-red-200 rounded-xl p-4 mb-6">
            <p className="text-red-600">{error}</p>
          </div>
        )}

        {/* Loading State */}
        {loading && (
          <div className="flex justify-center py-12">
            <div className="flex items-center space-x-3">
              <Loader2 className="w-6 h-6 animate-spin text-blue-600" />
              <span className="text-gray-600">Loading jokes...</span>
            </div>
          </div>
        )}

        {/* Jokes Grid */}
        {!loading && jokes.length > 0 && (
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {jokes.map((joke) => (
              <div key={joke.id} className="bg-white/70 backdrop-blur-sm rounded-2xl p-6 shadow-xl border border-white/20 hover:shadow-2xl transition-all">
                <div className="mb-4">
                  <p className="text-gray-800 leading-relaxed">{joke.content}</p>
                </div>
                
                <div className="flex items-center justify-between text-sm text-gray-500 mb-3">
                  <div className="flex items-center space-x-4">
                    <div className="flex items-center space-x-1">
                      <Laugh className="w-4 h-4" />
                      <span>{joke.laughScore ?? 'N/A'}</span>
                    </div>
                    {joke.type && (
                      <div className="bg-blue-100 text-blue-700 px-2 py-1 rounded-full text-xs">
                        Type {joke.type}
                      </div>
                    )}
                  </div>
                  <div className="text-xs">
                    {formatDate(joke.createdAt)}
                  </div>
                </div>

                {/* Author info */}
                <div className="text-xs text-gray-500 mb-3">
                  by {joke.author.name}
                </div>

                {joke.tags && joke.tags.length > 0 && (
                  <div className="flex flex-wrap gap-1">
                    {joke.tags.map((tag, index) => (
                      <span key={index} className="bg-gray-100 text-gray-600 px-2 py-1 rounded-full text-xs flex items-center space-x-1">
                        <TagIcon className="w-3 h-3" />
                        <span>{tag}</span>
                      </span>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        )}

        {/* Empty State */}
        {!loading && jokes.length === 0 && !error && (
          <div className="text-center py-12">
            <Laugh className="w-16 h-16 text-gray-300 mx-auto mb-4" />
            <h3 className="text-xl font-medium text-gray-600 mb-2">No jokes found</h3>
            <p className="text-gray-500">
              {searchQuery ? 'Try adjusting your search query' : 'No jokes are available right now'}
            </p>
          </div>
        )}

        {/* Pagination */}
        {!loading && totalPages > 1 && (
          <div className="flex justify-center mt-8">
            <div className="bg-white/70 backdrop-blur-sm rounded-2xl p-2 shadow-xl border border-white/20 flex items-center space-x-2">
                             <button
                 onClick={() => handlePageChange(currentPage - 1)}
                 disabled={currentPage === 1}
                 className="p-2 rounded-lg hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                 title="Previous page"
               >
                 <ChevronLeft className="w-5 h-5" />
               </button>
              
              <div className="flex space-x-1">
                {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                  const pageNum = i + 1;
                  return (
                    <button
                      key={pageNum}
                      onClick={() => handlePageChange(pageNum)}
                      className={`px-3 py-2 rounded-lg font-medium transition-colors ${
                        currentPage === pageNum
                          ? 'bg-blue-600 text-white'
                          : 'hover:bg-gray-100 text-gray-600'
                      }`}
                    >
                      {pageNum}
                    </button>
                  );
                })}
              </div>
              
                             <button
                 onClick={() => handlePageChange(currentPage + 1)}
                 disabled={currentPage === totalPages}
                 className="p-2 rounded-lg hover:bg-gray-100 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                 title="Next page"
               >
                 <ChevronRight className="w-5 h-5" />
               </button>
            </div>
          </div>
        )}
      </main>
    </div>
    </>
  );
}; 