import React, { useState, useEffect } from 'react';
import { Search, Plus, Edit2, Trash2 } from 'lucide-react';
import { Link } from 'react-router-dom';
import type { Joke, FilterRequest, JokeType } from '../types';
import { getJokeTypeLabel, getJokeTypeOptions } from '../utils/jokeTypeUtils';
import PageLayout from '../components/PageLayout';
import apiService from '../services/api';
import toast from 'react-hot-toast';

const JokesPage: React.FC = () => {
  const [jokes, setJokes] = useState<Joke[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedType, setSelectedType] = useState<JokeType | ''>('');
  const [deleteLoading, setDeleteLoading] = useState<string | null>(null);

  const fetchJokes = async () => {
    try {
      setLoading(true);
      const filters: FilterRequest = {
        query: searchQuery || undefined,
        type: selectedType || undefined,
        active: true,
      };
      const fetchedJokes = await apiService.getJokes(filters);
      setJokes(fetchedJokes);
    } catch (error) {
      console.error('Failed to fetch jokes:', error);
      toast.error('Failed to fetch jokes');
    } finally {
      setLoading(false);
    }
  };

  const handleSearch = async (e: React.FormEvent) => {
    e.preventDefault();
    await fetchJokes();
  };

  const handleDelete = async (jokeId: string) => {
    if (!confirm('Are you sure you want to delete this joke?')) {
      return;
    }

    try {
      setDeleteLoading(jokeId);
      await apiService.deleteJoke(jokeId);
      setJokes(jokes.filter(joke => joke.id !== jokeId));
      toast.success('Joke deleted successfully');
    } catch (error) {
      console.error('Failed to delete joke:', error);
      toast.error('Failed to delete joke');
    } finally {
      setDeleteLoading(null);
    }
  };

  useEffect(() => {
    fetchJokes();
  }, []);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      fetchJokes();
    }, 500);

    return () => clearTimeout(timeoutId);
  }, [searchQuery, selectedType]);

  return (
    <PageLayout 
      maxWidth="6xl"
      title="All Jokes"
      subtitle="Discover and enjoy our collection of humor"
    >
      <div className="space-y-6">
        <div className="flex justify-end">
          <Link
            to="/create-joke"
            className="flex items-center space-x-2 bg-gradient-to-r from-blue-600 to-blue-700 text-white px-6 py-3 rounded-xl hover:from-blue-700 hover:to-blue-800 transition-all transform hover:scale-105 shadow-lg"
          >
            <Plus className="h-5 w-5" />
            <span className="font-medium">Create Joke</span>
          </Link>
        </div>

        {/* Search and Filter */}
        <div className="bg-gradient-to-r from-gray-50 to-gray-100 rounded-xl p-6 border border-gray-200">
        <form onSubmit={handleSearch} className="flex gap-4 mb-4">
          <div className="flex-1">
            <div className="relative">
              <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 h-4 w-4" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search jokes..."
                className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
            </div>
          </div>
          <select
            value={selectedType}
            onChange={(e) => setSelectedType(e.target.value as JokeType | '')}
            className="px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            title="Filter jokes by type"
          >
            <option value="">All Types</option>
            {getJokeTypeOptions().map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
          <button
            type="submit"
            className="px-6 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
          >
            Search
          </button>
        </form>
      </div>

      {/* Jokes List */}
      {loading ? (
        <div className="text-center py-8">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading jokes...</p>
        </div>
      ) : jokes.length === 0 ? (
        <div className="bg-white rounded-lg shadow-md p-8 text-center">
          <p className="text-gray-500 mb-4">No jokes found</p>
          <Link
            to="/create-joke"
            className="inline-flex items-center space-x-2 bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 transition-colors"
          >
            <Plus className="h-4 w-4" />
            <span>Create Your First Joke</span>
          </Link>
        </div>
      ) : (
        <div className="grid gap-6">
          {jokes.map((joke) => (
            <div key={joke.id} className="bg-white rounded-lg shadow-md p-6 joke-card">
              <div className="flex justify-between items-start mb-4">
                <div className="flex-1">
                  <div className="flex items-center space-x-2 mb-2">
                    <span className="bg-blue-100 text-blue-800 px-2 py-1 rounded text-sm">
                      {getJokeTypeLabel(joke.type)}
                    </span>
                    {joke.laughScore && (
                      <span className="bg-yellow-100 text-yellow-800 px-2 py-1 rounded text-sm">
                        Score: {joke.laughScore}/10
                      </span>
                    )}
                  </div>
                  <p className="text-lg text-gray-800 mb-3 leading-relaxed">
                    {joke.content}
                  </p>
                  {joke.tags.length > 0 && (
                    <div className="flex flex-wrap gap-1 mb-3">
                      {joke.tags.map((tag) => (
                        <span
                          key={tag.id}
                          className="bg-gray-100 text-gray-700 px-2 py-1 rounded text-sm"
                        >
                          #{tag.name}
                        </span>
                      ))}
                    </div>
                  )}
                  <div className="text-sm text-gray-500">
                    <p>By: {joke.user?.name || 'Unknown'}</p>
                    <p>Created: {new Date(joke.createdAt).toLocaleDateString()}</p>
                  </div>
                </div>
                <div className="flex space-x-2 ml-4">
                  <Link
                    to={`/edit-joke/${joke.id}`}
                    className="p-2 text-gray-400 hover:text-blue-600 transition-colors"
                    title="Edit joke"
                  >
                    <Edit2 className="h-4 w-4" />
                  </Link>
                  <button
                    onClick={() => handleDelete(joke.id)}
                    disabled={deleteLoading === joke.id}
                    className="p-2 text-gray-400 hover:text-red-600 transition-colors disabled:opacity-50"
                    title="Delete joke"
                  >
                    <Trash2 className="h-4 w-4" />
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
      </div>
    </PageLayout>
  );
};

export default JokesPage; 