import React, { useState, useEffect } from 'react';
import { useAuthStore } from '../stores/authStore';
import { Calendar, Smile, Tag, TrendingUp, User as UserIcon } from 'lucide-react';
import type { Joke } from '../types';
import { getJokeTypeLabel } from '../utils/jokeTypeUtils';
import PageLayout from '../components/PageLayout';
import apiService from '../services/api';
import toast from 'react-hot-toast';

type UserStats = {
  totalJokes: number;
  activeJokes: number;
  averageLaughScore: number;
  totalTags: number;
  favoriteJokeType: string;
  joinDate: string;
}

const ProfilePage: React.FC = () => {
  const { user, logout } = useAuthStore();
  const [loading, setLoading] = useState(true);
  const [userJokes, setUserJokes] = useState<Joke[]>([]);
  const [stats, setStats] = useState<UserStats | null>(null);

  const fetchUserData = async () => {
    try {
      setLoading(true);
      
      // Fetch all jokes to filter user's jokes
      const allJokes = await apiService.getJokes();
      const userJokes = allJokes.filter(joke => joke.user?.id === user?.id);
      setUserJokes(userJokes);

      // Calculate user statistics
      const activeJokes = userJokes.filter(joke => joke.isActive);
      const jokesWithScores = userJokes.filter(joke => joke.laughScore);
      const averageScore = jokesWithScores.length > 0 
        ? jokesWithScores.reduce((sum, joke) => sum + (joke.laughScore || 0), 0) / jokesWithScores.length 
        : 0;

      // Count joke types to find favorite
      const typeCount: Record<string, number> = {};
      userJokes.forEach(joke => {
        typeCount[joke.type] = (typeCount[joke.type] || 0) + 1;
      });
      const favoriteType = Object.entries(typeCount).reduce((a, b) => 
        typeCount[a[0]] > typeCount[b[0]] ? a : b, ['None', 0])[0];

      // Get unique tags used by user
      const userTags = new Set();
      userJokes.forEach(joke => {
        joke.tags.forEach(tag => userTags.add(tag.id));
      });

      setStats({
        totalJokes: userJokes.length,
        activeJokes: activeJokes.length,
        averageLaughScore: Math.round(averageScore * 10) / 10,
        totalTags: userTags.size,
        favoriteJokeType: favoriteType,
        joinDate: user?.createdAt || new Date().toISOString(),
      });

    } catch (error) {
      console.error('Failed to fetch user data:', error);
      toast.error('Failed to load profile data');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (user) {
      fetchUserData();
    }
  }, [user]);

  const handleLogout = async () => {
    if (confirm('Are you sure you want to log out?')) {
      await logout();
    }
  };

  if (!user) {
    return (
      <div className="max-w-4xl mx-auto">
        <div className="bg-white rounded-lg shadow-md p-8 text-center">
          <p className="text-gray-500">Please log in to view your profile.</p>
        </div>
      </div>
    );
  }

  return (
    <PageLayout 
      maxWidth="6xl"
      title={`${user.name}'s Profile`}
      subtitle="Your comedy journey and statistics"
    >
      <div className="space-y-8">
        {/* Profile Header */}
        <div className="bg-gradient-to-r from-blue-50 to-indigo-50 rounded-xl p-6 border border-blue-200">
        <div className="flex items-center space-x-6">
          <div className="h-20 w-20 bg-gradient-to-br from-blue-500 to-purple-600 rounded-full flex items-center justify-center">
            <UserIcon className="h-10 w-10 text-white" />
          </div>
          <div className="flex-1">
            <h1 className="text-3xl font-bold text-gray-900">{user.name}</h1>
            <p className="text-gray-600">{user.email}</p>
            <div className="flex items-center space-x-2 mt-2">
              <Calendar className="h-4 w-4 text-gray-400" />
              <span className="text-sm text-gray-500">
                Joined {stats ? new Date(stats.joinDate).toLocaleDateString() : 'Unknown'}
              </span>
            </div>
            {user.role && (
              <span className="inline-block mt-2 px-2 py-1 bg-blue-100 text-blue-800 rounded text-xs font-medium">
                {user.role}
              </span>
            )}
          </div>
          <button
            onClick={handleLogout}
            className="px-4 py-2 border border-red-300 text-red-700 rounded-md hover:bg-red-50 transition-colors"
          >
            Logout
          </button>
        </div>
      </div>

      {/* Statistics Cards */}
      {loading ? (
        <div className="text-center py-8">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading profile statistics...</p>
        </div>
      ) : stats && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          <div className="bg-white rounded-lg shadow-md p-6">
            <div className="flex items-center space-x-3">
              <div className="p-2 bg-blue-100 rounded-lg">
                <Smile className="h-6 w-6 text-blue-600" />
              </div>
              <div>
                <p className="text-sm font-medium text-gray-600">Total Jokes</p>
                <p className="text-2xl font-bold text-gray-900">{stats.totalJokes}</p>
              </div>
            </div>
          </div>

          <div className="bg-white rounded-lg shadow-md p-6">
            <div className="flex items-center space-x-3">
              <div className="p-2 bg-green-100 rounded-lg">
                <TrendingUp className="h-6 w-6 text-green-600" />
              </div>
              <div>
                <p className="text-sm font-medium text-gray-600">Active Jokes</p>
                <p className="text-2xl font-bold text-gray-900">{stats.activeJokes}</p>
              </div>
            </div>
          </div>

          <div className="bg-white rounded-lg shadow-md p-6">
            <div className="flex items-center space-x-3">
              <div className="p-2 bg-yellow-100 rounded-lg">
                <TrendingUp className="h-6 w-6 text-yellow-600" />
              </div>
              <div>
                <p className="text-sm font-medium text-gray-600">Avg. Score</p>
                <p className="text-2xl font-bold text-gray-900">
                  {stats.averageLaughScore > 0 ? `${stats.averageLaughScore}/10` : 'N/A'}
                </p>
              </div>
            </div>
          </div>

          <div className="bg-white rounded-lg shadow-md p-6">
            <div className="flex items-center space-x-3">
              <div className="p-2 bg-purple-100 rounded-lg">
                <Tag className="h-6 w-6 text-purple-600" />
              </div>
              <div>
                <p className="text-sm font-medium text-gray-600">Tags Used</p>
                <p className="text-2xl font-bold text-gray-900">{stats.totalTags}</p>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Additional Info */}
      {stats && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div className="bg-white rounded-lg shadow-md p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">Favorite Joke Type</h3>
            <div className="flex items-center space-x-3">
              <span className="bg-blue-100 text-blue-800 px-3 py-1 rounded-full text-sm font-medium">
                {getJokeTypeLabel(parseInt(stats.favoriteJokeType))}
              </span>
            </div>
          </div>

          <div className="bg-white rounded-lg shadow-md p-6">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">Account Status</h3>
            <div className="space-y-2">
              <div className="flex justify-between">
                <span className="text-gray-600">Account Status:</span>
                <span className="bg-green-100 text-green-800 px-2 py-1 rounded text-xs font-medium">
                  Active
                </span>
              </div>
              <div className="flex justify-between">
                <span className="text-gray-600">Email Verified:</span>
                <span className="bg-green-100 text-green-800 px-2 py-1 rounded text-xs font-medium">
                  Yes
                </span>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Recent Jokes */}
      <div className="bg-white rounded-lg shadow-md p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">Your Recent Jokes</h3>
        {userJokes.length === 0 ? (
          <div className="text-center py-8">
            <Smile className="h-12 w-12 text-gray-300 mx-auto mb-4" />
            <p className="text-gray-500">You haven't created any jokes yet!</p>
            <p className="text-sm text-gray-400 mt-1">Start sharing your humor with the community</p>
          </div>
        ) : (
          <div className="space-y-4">
            {userJokes.slice(0, 3).map((joke) => (
              <div key={joke.id} className="border border-gray-200 rounded-lg p-4">
                <div className="flex justify-between items-start mb-2">
                  <span className="bg-blue-100 text-blue-800 px-2 py-1 rounded text-xs">
                    {getJokeTypeLabel(joke.type)}
                  </span>
                  <span className={`px-2 py-1 rounded text-xs ${
                    joke.isActive 
                      ? 'bg-green-100 text-green-800' 
                      : 'bg-gray-100 text-gray-600'
                  }`}>
                    {joke.isActive ? 'Active' : 'Inactive'}
                  </span>
                </div>
                <p className="text-gray-800 mb-2">{joke.content.slice(0, 150)}...</p>
                <div className="flex justify-between items-center text-sm text-gray-500">
                  <span>{new Date(joke.createdAt).toLocaleDateString()}</span>
                  {joke.laughScore && (
                    <span className="bg-yellow-100 text-yellow-800 px-2 py-1 rounded">
                      Score: {joke.laughScore}/10
                    </span>
                  )}
                </div>
              </div>
            ))}
            {userJokes.length > 3 && (
              <p className="text-center text-sm text-gray-500">
                ...and {userJokes.length - 3} more jokes
              </p>
            )}
          </div>
        )}
        </div>
      </div>
    </PageLayout>
  );
};

export default ProfilePage; 