import React from 'react';
import { useAuthStore } from '../store/authStore';
import { useNavigate } from 'react-router-dom';
import { 
  User, 
  Shield, 
  LogOut, 
  Settings, 
  Crown, 
  UserCheck,
  Activity,
  Clock,
  Mail
} from 'lucide-react';

export const Dashboard: React.FC = () => {
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  const getRoleIcon = (role: string) => {
    switch (role) {
      case 'admin':
        return <Crown className="w-5 h-5 text-yellow-500" />;
      case 'moderator':
        return <Shield className="w-5 h-5 text-blue-500" />;
      default:
        return <User className="w-5 h-5 text-gray-500" />;
    }
  };

  const getRoleBadgeColor = (role: string) => {
    switch (role) {
      case 'admin':
        return 'bg-yellow-100 text-yellow-800 border-yellow-200';
      case 'moderator':
        return 'bg-blue-100 text-blue-800 border-blue-200';
      default:
        return 'bg-gray-100 text-gray-800 border-gray-200';
    }
  };

  if (!user) {
    return null;
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50">
      {/* Header */}
      <header className="bg-white/70 backdrop-blur-sm border-b border-white/20 sticky top-0 z-10">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center h-16">
            <div className="flex items-center space-x-3">
              <div className="bg-gradient-to-r from-blue-600 to-purple-600 w-10 h-10 rounded-lg flex items-center justify-center">
                <Shield className="w-6 h-6 text-white" />
              </div>
              <h1 className="text-xl font-bold text-gray-900">WhateverAPI</h1>
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
        {/* Welcome Section */}
        <div className="bg-white/70 backdrop-blur-sm rounded-2xl p-8 shadow-xl border border-white/20 mb-8">
          <div className="flex items-center justify-between">
            <div>
              <h2 className="text-3xl font-bold text-gray-900 mb-2">
                Welcome back, {user.name}!
              </h2>
              <p className="text-gray-600">
                You're successfully authenticated with WhateverAPI
              </p>
            </div>
            
            <div className={`px-4 py-2 rounded-full border ${getRoleBadgeColor(user.role)} font-medium text-sm flex items-center space-x-2`}>
              {getRoleIcon(user.role)}
              <span className="capitalize">{user.role}</span>
            </div>
          </div>
        </div>

        {/* Stats Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
          <div className="bg-white/70 backdrop-blur-sm rounded-xl p-6 shadow-lg border border-white/20">
            <div className="flex items-center space-x-3">
              <div className="bg-blue-100 p-3 rounded-lg">
                <UserCheck className="w-6 h-6 text-blue-600" />
              </div>
              <div>
                <p className="text-sm font-medium text-gray-600">Status</p>
                <p className="text-xl font-bold text-gray-900">Authenticated</p>
              </div>
            </div>
          </div>

          <div className="bg-white/70 backdrop-blur-sm rounded-xl p-6 shadow-lg border border-white/20">
            <div className="flex items-center space-x-3">
              <div className="bg-green-100 p-3 rounded-lg">
                <Activity className="w-6 h-6 text-green-600" />
              </div>
              <div>
                <p className="text-sm font-medium text-gray-600">Session</p>
                <p className="text-xl font-bold text-gray-900">Active</p>
              </div>
            </div>
          </div>

          <div className="bg-white/70 backdrop-blur-sm rounded-xl p-6 shadow-lg border border-white/20">
            <div className="flex items-center space-x-3">
              <div className="bg-purple-100 p-3 rounded-lg">
                <Shield className="w-6 h-6 text-purple-600" />
              </div>
              <div>
                <p className="text-sm font-medium text-gray-600">Role</p>
                <p className="text-xl font-bold text-gray-900 capitalize">{user.role}</p>
              </div>
            </div>
          </div>

          <div className="bg-white/70 backdrop-blur-sm rounded-xl p-6 shadow-lg border border-white/20">
            <div className="flex items-center space-x-3">
              <div className="bg-orange-100 p-3 rounded-lg">
                <Clock className="w-6 h-6 text-orange-600" />
              </div>
              <div>
                <p className="text-sm font-medium text-gray-600">Last Login</p>
                <p className="text-xl font-bold text-gray-900">Now</p>
              </div>
            </div>
          </div>
        </div>

        {/* User Profile Card */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
          <div className="bg-white/70 backdrop-blur-sm rounded-2xl p-8 shadow-xl border border-white/20">
            <h3 className="text-2xl font-bold text-gray-900 mb-6 flex items-center space-x-3">
              <User className="w-6 h-6" />
              <span>Profile Information</span>
            </h3>
            
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-600 mb-1">User ID</label>
                <div className="bg-gray-50 rounded-lg p-3 font-mono text-sm text-gray-800">
                  {user.userId}
                </div>
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-600 mb-1">Email</label>
                <div className="bg-gray-50 rounded-lg p-3 flex items-center space-x-2">
                  <Mail className="w-4 h-4 text-gray-500" />
                  <span className="text-gray-800">{user.email}</span>
                </div>
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-600 mb-1">Role</label>
                <div className="bg-gray-50 rounded-lg p-3 flex items-center space-x-2">
                  {getRoleIcon(user.role)}
                  <span className="text-gray-800 capitalize">{user.role}</span>
                </div>
              </div>
            </div>
          </div>

          {/* Quick Actions */}
          <div className="bg-white/70 backdrop-blur-sm rounded-2xl p-8 shadow-xl border border-white/20">
            <h3 className="text-2xl font-bold text-gray-900 mb-6 flex items-center space-x-3">
              <Settings className="w-6 h-6" />
              <span>Quick Actions</span>
            </h3>
            
            <div className="space-y-4">
              <button className="w-full bg-blue-50 hover:bg-blue-100 text-blue-700 p-4 rounded-xl text-left transition-colors flex items-center space-x-3">
                <Settings className="w-5 h-5" />
                <div>
                  <div className="font-medium">Account Settings</div>
                  <div className="text-sm text-blue-600">Manage your account preferences</div>
                </div>
              </button>
              
              <button className="w-full bg-green-50 hover:bg-green-100 text-green-700 p-4 rounded-xl text-left transition-colors flex items-center space-x-3">
                <Shield className="w-5 h-5" />
                <div>
                  <div className="font-medium">Security</div>
                  <div className="text-sm text-green-600">View security settings and activity</div>
                </div>
              </button>
              
              <button 
                onClick={handleLogout}
                className="w-full bg-red-50 hover:bg-red-100 text-red-700 p-4 rounded-xl text-left transition-colors flex items-center space-x-3"
              >
                <LogOut className="w-5 h-5" />
                <div>
                  <div className="font-medium">Sign Out</div>
                  <div className="text-sm text-red-600">End your current session</div>
                </div>
              </button>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};