import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { RefreshCw, Plus, TrendingUp, Users, Smile, Star } from 'lucide-react';
import { useAuthStore } from '../stores/authStore';
import PageLayout from '../components/PageLayout';
import type { Joke } from '../types';
import apiService from '../services/api';
import JokeCard from '../components/JokeCard';
import toast from 'react-hot-toast';

const HomePage: React.FC = () => {
  const { user } = useAuthStore();
  const [randomJoke, setRandomJoke] = useState<Joke | null>(null);
  const [recentJokes, setRecentJokes] = useState<Joke[]>([]);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [stats, setStats] = useState({
    totalJokes: 0,
    totalUsers: 0,
    averageScore: 0,
  });

  const fetchData = async () => {
    try {
      const jokes = await apiService.getJokes();
      
      if (jokes.length > 0) {
        // Get random joke
        const randomIndex = Math.floor(Math.random() * jokes.length);
        setRandomJoke(jokes[randomIndex]);
        
        // Get recent jokes (last 3)
        const sortedJokes = jokes
          .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
          .slice(0, 3);
        setRecentJokes(sortedJokes);
        
        // Calculate stats
        const activeJokes = jokes.filter(joke => joke.isActive);
        const jokesWithScores = jokes.filter(joke => joke.laughScore);
        const averageScore = jokesWithScores.length > 0 
          ? jokesWithScores.reduce((sum, joke) => sum + (joke.laughScore || 0), 0) / jokesWithScores.length 
          : 0;
        
        const uniqueUsers = new Set(jokes.map(joke => joke.user?.id).filter(Boolean));
        
        setStats({
          totalJokes: activeJokes.length,
          totalUsers: uniqueUsers.size,
          averageScore: Math.round(averageScore * 10) / 10,
        });
      }
    } catch (error) {
      console.error('Failed to fetch data:', error);
      toast.error('Failed to load jokes');
    } finally {
      setLoading(false);
      setRefreshing(false);
    }
  };

  useEffect(() => {
    fetchData();
  }, []);

  const handleRefresh = async () => {
    setRefreshing(true);
    await fetchData();
    toast.success('Fresh joke loaded!');
  };

  if (loading) {
    return (
      <div className="text-center py-8">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
        <p className="mt-4 text-gray-600">Loading your daily dose of humor...</p>
      </div>
    );
  }

  return (
    <PageLayout 
      maxWidth="6xl"
      title={`Welcome back, ${user?.name?.split(' ')[0]}! 🎭`}
      subtitle="Ready to brighten someone's day with humor?"
    >
      <div className="space-y-8">

      {/* Stats Cards */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <div className="bg-gradient-to-br from-blue-50 to-blue-100 rounded-xl border border-blue-200 p-6 hover:shadow-md transition-shadow">
          <div className="flex items-center space-x-3">
            <div className="p-3 bg-blue-500 rounded-xl shadow-lg">
              <Smile className="h-6 w-6 text-white" />
            </div>
            <div>
              <p className="text-sm font-medium text-blue-700">Total Jokes</p>
              <p className="text-2xl font-bold text-blue-900">{stats.totalJokes}</p>
            </div>
          </div>
        </div>

        <div className="bg-gradient-to-br from-green-50 to-green-100 rounded-xl border border-green-200 p-6 hover:shadow-md transition-shadow">
          <div className="flex items-center space-x-3">
            <div className="p-3 bg-green-500 rounded-xl shadow-lg">
              <Users className="h-6 w-6 text-white" />
            </div>
            <div>
              <p className="text-sm font-medium text-green-700">Active Users</p>
              <p className="text-2xl font-bold text-green-900">{stats.totalUsers}</p>
            </div>
          </div>
        </div>

        <div className="bg-gradient-to-br from-yellow-50 to-yellow-100 rounded-xl border border-yellow-200 p-6 hover:shadow-md transition-shadow">
          <div className="flex items-center space-x-3">
            <div className="p-3 bg-yellow-500 rounded-xl shadow-lg">
              <Star className="h-6 w-6 text-white" />
            </div>
            <div>
              <p className="text-sm font-medium text-yellow-700">Avg. Score</p>
              <p className="text-2xl font-bold text-yellow-900">
                {stats.averageScore > 0 ? `${stats.averageScore}/10` : 'N/A'}
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* Random Joke Section */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-2xl font-bold text-gray-900 flex items-center space-x-2">
            <TrendingUp className="h-6 w-6 text-blue-600" />
            <span>Random Joke</span>
          </h2>
          <button
            onClick={handleRefresh}
            disabled={refreshing}
            className="flex items-center space-x-2 bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 disabled:opacity-50 transition-colors"
          >
            <RefreshCw className={`h-4 w-4 ${refreshing ? 'animate-spin' : ''}`} />
            <span>New Joke</span>
          </button>
        </div>
        
        {randomJoke ? (
          <JokeCard joke={randomJoke} showActions={false} />
        ) : (
          <div className="bg-white rounded-lg shadow-md p-8 text-center">
            <Smile className="h-12 w-12 text-gray-300 mx-auto mb-4" />
            <p className="text-gray-500">No jokes available yet!</p>
            <Link
              to="/create-joke"
              className="inline-flex items-center space-x-2 mt-4 bg-blue-600 text-white px-4 py-2 rounded-md hover:bg-blue-700 transition-colors"
            >
              <Plus className="h-4 w-4" />
              <span>Create the First Joke</span>
            </Link>
          </div>
        )}
      </div>

      {/* Quick Actions */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        <Link
          to="/create-joke"
          className="bg-gradient-to-r from-blue-500 to-blue-600 text-white rounded-lg p-6 hover:from-blue-600 hover:to-blue-700 transition-all transform hover:scale-105 shadow-lg"
        >
          <div className="flex items-center space-x-3">
            <Plus className="h-8 w-8" />
            <div>
              <h3 className="text-lg font-semibold">Create Joke</h3>
              <p className="text-blue-100">Share your humor</p>
            </div>
          </div>
        </Link>

        <Link
          to="/jokes"
          className="bg-gradient-to-r from-green-500 to-green-600 text-white rounded-lg p-6 hover:from-green-600 hover:to-green-700 transition-all transform hover:scale-105 shadow-lg"
        >
          <div className="flex items-center space-x-3">
            <Smile className="h-8 w-8" />
            <div>
              <h3 className="text-lg font-semibold">Browse Jokes</h3>
              <p className="text-green-100">Discover comedy</p>
            </div>
          </div>
        </Link>

        <Link
          to="/profile"
          className="bg-gradient-to-r from-purple-500 to-purple-600 text-white rounded-lg p-6 hover:from-purple-600 hover:to-purple-700 transition-all transform hover:scale-105 shadow-lg"
        >
          <div className="flex items-center space-x-3">
            <Users className="h-8 w-8" />
            <div>
              <h3 className="text-lg font-semibold">Your Profile</h3>
              <p className="text-purple-100">View your stats</p>
            </div>
          </div>
        </Link>
      </div>

      {/* Recent Jokes */}
      {recentJokes.length > 0 && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-2xl font-bold text-gray-900">Recent Jokes</h2>
            <Link
              to="/jokes"
              className="text-blue-600 hover:text-blue-700 font-medium"
            >
              View All →
            </Link>
          </div>
          
          <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-6">
            {recentJokes.map((joke) => (
              <JokeCard key={joke.id} joke={joke} showActions={false} />
            ))}
          </div>
        </div>
      )}
      </div>
    </PageLayout>
  );
};

export default HomePage; 