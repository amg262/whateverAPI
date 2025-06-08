import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../stores/authStore';
import { LogOut, User, Smile } from 'lucide-react';

const Navbar: React.FC = () => {
  const { user, isAuthenticated, logout } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  if (!isAuthenticated) {
    return null;
  }

  return (
    <nav className="bg-white/95 backdrop-blur-sm shadow-lg border-b border-gray-200/50 sticky top-0 z-50">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">
          <div className="flex items-center space-x-8">
            <Link to="/" className="flex items-center space-x-2 text-xl font-bold bg-gradient-to-r from-blue-600 to-indigo-600 bg-clip-text text-transparent">
              <div className="p-2 bg-gradient-to-r from-blue-500 to-indigo-600 rounded-xl shadow-lg">
                <Smile className="h-6 w-6 text-white" />
              </div>
              <span>JokeApp</span>
            </Link>
            <div className="hidden md:flex space-x-4">
              <Link 
                to="/" 
                className="text-gray-600 hover:text-gray-900 px-3 py-2 rounded-md text-sm font-medium transition-colors"
              >
                Home
              </Link>
              <Link 
                to="/jokes" 
                className="text-gray-600 hover:text-gray-900 px-3 py-2 rounded-md text-sm font-medium transition-colors"
              >
                All Jokes
              </Link>
              <Link 
                to="/create-joke" 
                className="text-gray-600 hover:text-gray-900 px-3 py-2 rounded-md text-sm font-medium transition-colors"
              >
                Create Joke
              </Link>
              <Link 
                to="/tags" 
                className="text-gray-600 hover:text-gray-900 px-3 py-2 rounded-md text-sm font-medium transition-colors"
              >
                Tags
              </Link>
            </div>
          </div>
          
          <div className="flex items-center space-x-4">
            <Link
              to="/profile"
              className="flex items-center space-x-2 text-sm text-gray-600 hover:text-gray-900 px-3 py-2 rounded-md transition-colors"
            >
              <User className="h-4 w-4" />
              <span className="font-medium">{user?.name}</span>
            </Link>
            <button
              onClick={handleLogout}
              className="flex items-center space-x-1 text-gray-600 hover:text-red-600 px-3 py-2 rounded-md text-sm font-medium transition-colors"
            >
              <LogOut className="h-4 w-4" />
              <span>Logout</span>
            </button>
          </div>
        </div>
        
        {/* Mobile Navigation */}
        <div className="md:hidden border-t border-gray-200 py-3">
          <div className="flex flex-wrap justify-center space-x-4">
            <Link 
              to="/" 
              className="text-gray-600 hover:text-gray-900 px-2 py-1 rounded text-sm transition-colors"
            >
              Home
            </Link>
            <Link 
              to="/jokes" 
              className="text-gray-600 hover:text-gray-900 px-2 py-1 rounded text-sm transition-colors"
            >
              Jokes
            </Link>
            <Link 
              to="/create-joke" 
              className="text-gray-600 hover:text-gray-900 px-2 py-1 rounded text-sm transition-colors"
            >
              Create
            </Link>
            <Link 
              to="/tags" 
              className="text-gray-600 hover:text-gray-900 px-2 py-1 rounded text-sm transition-colors"
            >
              Tags
            </Link>
            <Link 
              to="/profile" 
              className="text-gray-600 hover:text-gray-900 px-2 py-1 rounded text-sm transition-colors"
            >
              Profile
            </Link>
          </div>
        </div>
      </div>
    </nav>
  );
};

export default Navbar; 