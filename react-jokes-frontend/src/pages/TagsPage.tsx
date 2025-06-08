import React, { useState, useEffect } from 'react';
import { Plus, Edit2, Trash2, Save, X } from 'lucide-react';
import type { Tag, CreateTagRequest, UpdateTagRequest } from '../types';
import apiService from '../services/api';
import toast from 'react-hot-toast';

const TagsPage: React.FC = () => {
  const [tags, setTags] = useState<Tag[]>([]);
  const [loading, setLoading] = useState(true);
  const [editingTag, setEditingTag] = useState<string | null>(null);
  const [editName, setEditName] = useState('');
  const [newTagName, setNewTagName] = useState('');
  const [createLoading, setCreateLoading] = useState(false);
  const [deleteLoading, setDeleteLoading] = useState<string | null>(null);

  const fetchTags = async () => {
    try {
      setLoading(true);
      const fetchedTags = await apiService.getTags();
      setTags(fetchedTags);
    } catch (error) {
      console.error('Failed to fetch tags:', error);
      toast.error('Failed to fetch tags');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateTag = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!newTagName.trim()) {
      toast.error('Please enter a tag name');
      return;
    }

    try {
      setCreateLoading(true);
      const request: CreateTagRequest = { name: newTagName.trim() };
      const newTag = await apiService.createTag(request);
      setTags([...tags, newTag]);
      setNewTagName('');
      toast.success('Tag created successfully!');
    } catch (error) {
      console.error('Failed to create tag:', error);
      toast.error('Failed to create tag');
    } finally {
      setCreateLoading(false);
    }
  };

  const handleEditStart = (tag: Tag) => {
    setEditingTag(tag.id);
    setEditName(tag.name);
  };

  const handleEditCancel = () => {
    setEditingTag(null);
    setEditName('');
  };

  const handleEditSave = async (tagId: string) => {
    if (!editName.trim()) {
      toast.error('Please enter a tag name');
      return;
    }

    try {
      const tag = tags.find(t => t.id === tagId);
      if (!tag) return;

      const request: UpdateTagRequest = { 
        name: editName.trim(),
        isActive: tag.isActive 
      };
      const updatedTag = await apiService.updateTag(tagId, request);
      setTags(tags.map(t => t.id === tagId ? updatedTag : t));
      setEditingTag(null);
      setEditName('');
      toast.success('Tag updated successfully!');
    } catch (error) {
      console.error('Failed to update tag:', error);
      toast.error('Failed to update tag');
    }
  };

  const handleToggleActive = async (tag: Tag) => {
    try {
      const request: UpdateTagRequest = { 
        name: tag.name,
        isActive: !tag.isActive 
      };
      const updatedTag = await apiService.updateTag(tag.id, request);
      setTags(tags.map(t => t.id === tag.id ? updatedTag : t));
      toast.success(`Tag ${updatedTag.isActive ? 'activated' : 'deactivated'} successfully!`);
    } catch (error) {
      console.error('Failed to toggle tag status:', error);
      toast.error('Failed to update tag status');
    }
  };

  const handleDelete = async (tagId: string) => {
    if (!confirm('Are you sure you want to delete this tag?')) {
      return;
    }

    try {
      setDeleteLoading(tagId);
      await apiService.deleteTag(tagId);
      setTags(tags.filter(tag => tag.id !== tagId));
      toast.success('Tag deleted successfully');
    } catch (error) {
      console.error('Failed to delete tag:', error);
      toast.error('Failed to delete tag');
    } finally {
      setDeleteLoading(null);
    }
  };

  useEffect(() => {
    fetchTags();
  }, []);

  return (
    <div className="max-w-4xl mx-auto">
      <div className="mb-6">
        <h1 className="text-3xl font-bold text-gray-900 mb-2">Manage Tags</h1>
        <p className="text-gray-600">Create and organize tags to categorize your jokes</p>
      </div>

      {/* Create New Tag */}
      <div className="bg-white rounded-lg shadow-md p-6 mb-6">
        <h2 className="text-xl font-semibold text-gray-800 mb-4">Create New Tag</h2>
        <form onSubmit={handleCreateTag} className="flex gap-4">
          <div className="flex-1">
            <input
              type="text"
              value={newTagName}
              onChange={(e) => setNewTagName(e.target.value)}
              placeholder="Enter tag name..."
              className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
            />
          </div>
          <button
            type="submit"
            disabled={createLoading || !newTagName.trim()}
            className="flex items-center space-x-2 bg-blue-600 text-white px-6 py-2 rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            <Plus className="h-4 w-4" />
            <span>{createLoading ? 'Creating...' : 'Create Tag'}</span>
          </button>
        </form>
      </div>

      {/* Tags List */}
      {loading ? (
        <div className="text-center py-8">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading tags...</p>
        </div>
      ) : tags.length === 0 ? (
        <div className="bg-white rounded-lg shadow-md p-8 text-center">
          <p className="text-gray-500 mb-4">No tags found</p>
          <p className="text-sm text-gray-400">Create your first tag to get started!</p>
        </div>
      ) : (
        <div className="bg-white rounded-lg shadow-md overflow-hidden">
          <div className="px-6 py-4 bg-gray-50 border-b border-gray-200">
            <h2 className="text-lg font-semibold text-gray-800">All Tags ({tags.length})</h2>
          </div>
          <div className="divide-y divide-gray-200">
            {tags.map((tag) => (
              <div key={tag.id} className="px-6 py-4 flex items-center justify-between">
                <div className="flex items-center space-x-4 flex-1">
                  {editingTag === tag.id ? (
                    <div className="flex items-center space-x-2 flex-1">
                                             <input
                         type="text"
                         value={editName}
                         onChange={(e) => setEditName(e.target.value)}
                         className="flex-1 px-3 py-1 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                         onKeyDown={(e) => {
                           if (e.key === 'Enter') {
                             handleEditSave(tag.id);
                           } else if (e.key === 'Escape') {
                             handleEditCancel();
                           }
                         }}
                         autoFocus
                         placeholder="Edit tag name"
                         title="Edit tag name"
                       />
                      <button
                        onClick={() => handleEditSave(tag.id)}
                        className="p-1 text-green-600 hover:text-green-800 transition-colors"
                        title="Save changes"
                      >
                        <Save className="h-4 w-4" />
                      </button>
                      <button
                        onClick={handleEditCancel}
                        className="p-1 text-gray-400 hover:text-gray-600 transition-colors"
                        title="Cancel editing"
                      >
                        <X className="h-4 w-4" />
                      </button>
                    </div>
                  ) : (
                    <>
                      <div className="flex items-center space-x-3">
                        <span className={`inline-flex items-center px-3 py-1 rounded-full text-sm font-medium ${
                          tag.isActive 
                            ? 'bg-blue-100 text-blue-800' 
                            : 'bg-gray-100 text-gray-600'
                        }`}>
                          #{tag.name}
                        </span>
                        <span className={`px-2 py-1 rounded text-xs ${
                          tag.isActive 
                            ? 'bg-green-100 text-green-800' 
                            : 'bg-red-100 text-red-800'
                        }`}>
                          {tag.isActive ? 'Active' : 'Inactive'}
                        </span>
                      </div>
                      <div className="text-sm text-gray-500">
                        Created: {new Date(tag.createdAt).toLocaleDateString()}
                      </div>
                    </>
                  )}
                </div>
                
                {editingTag !== tag.id && (
                  <div className="flex items-center space-x-2">
                    <button
                      onClick={() => handleToggleActive(tag)}
                      className={`px-3 py-1 rounded text-xs font-medium transition-colors ${
                        tag.isActive
                          ? 'bg-yellow-100 text-yellow-800 hover:bg-yellow-200'
                          : 'bg-green-100 text-green-800 hover:bg-green-200'
                      }`}
                    >
                      {tag.isActive ? 'Deactivate' : 'Activate'}
                    </button>
                    <button
                      onClick={() => handleEditStart(tag)}
                      className="p-2 text-gray-400 hover:text-blue-600 transition-colors"
                      title="Edit tag"
                    >
                      <Edit2 className="h-4 w-4" />
                    </button>
                    <button
                      onClick={() => handleDelete(tag.id)}
                      disabled={deleteLoading === tag.id}
                      className="p-2 text-gray-400 hover:text-red-600 transition-colors disabled:opacity-50"
                      title="Delete tag"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
};

export default TagsPage; 