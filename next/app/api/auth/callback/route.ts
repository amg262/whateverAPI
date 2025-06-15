import { NextRequest, NextResponse } from 'next/server';

export async function GET(request: NextRequest) {
  const { searchParams } = new URL(request.url);
  
  // Get all query parameters from the OAuth callback
  const token = searchParams.get('token');
  const user = searchParams.get('user');
  const error = searchParams.get('error');
  
  // Create the redirect URL to our frontend callback page
  const callbackUrl = new URL('/auth/callback', request.url);
  
  // Forward all the parameters to our frontend callback
  if (token) callbackUrl.searchParams.set('token', token);
  if (user) callbackUrl.searchParams.set('user', user);
  if (error) callbackUrl.searchParams.set('error', error);
  
  // Redirect to our frontend callback page
  return NextResponse.redirect(callbackUrl);
} 