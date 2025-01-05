#!/usr/bin/env python3
import argparse
import asyncio
import aiohttp
import time
import json
from datetime import datetime
from typing import Optional, Dict, List


class APITester:
    def __init__(self, base_url: str, interval: float = 1.0, max_requests: Optional[int] = None):
        """Initialize the API tester with configuration parameters.

        Args:
            base_url: The base URL of the API to test
            interval: Time between requests in seconds
            max_requests: Maximum number of requests to make (None for unlimited)
        """
        self.base_url = base_url.rstrip('/')
        self.interval = interval
        self.max_requests = max_requests
        self.request_count = 0
        self.success_count = 0
        self.error_count = 0
        self.response_times: List[float] = []
        self.start_time = None

    async def make_request(self, session: aiohttp.ClientSession) -> Dict:
        """Make a single request to the API and track its results."""
        start_time = time.time()
        try:
            async with session.get(f"{self.base_url}/api/jokes/whatever") as response:
                elapsed = time.time() - start_time
                self.response_times.append(elapsed)

                status = response.status
                if status == 200:
                    self.success_count += 1
                    data = await response.json()
                else:
                    self.error_count += 1
                    data = await response.text()

                return {
                    "timestamp": datetime.now().isoformat(),
                    "status": status,
                    "elapsed": elapsed,
                    "response": data
                }
        except Exception as e:
            self.error_count += 1
            elapsed = time.time() - start_time
            self.response_times.append(elapsed)
            return {
                "timestamp": datetime.now().isoformat(),
                "status": "ERROR",
                "elapsed": elapsed,
                "error": str(e)
            }

    def print_stats(self):
        """Print current testing statistics."""
        if not self.response_times:
            return

        avg_response = sum(self.response_times) / len(self.response_times)
        max_response = max(self.response_times)
        min_response = min(self.response_times)

        elapsed = time.time() - self.start_time
        requests_per_second = self.request_count / elapsed

        print("\n=== API Test Statistics ===")
        print(f"Total Requests: {self.request_count}")
        print(f"Successful: {self.success_count}")
        print(f"Errors: {self.error_count}")
        print(f"Average Response Time: {avg_response:.3f}s")
        print(f"Max Response Time: {max_response:.3f}s")
        print(f"Min Response Time: {min_response:.3f}s")
        print(f"Requests/second: {requests_per_second:.2f}")
        print("========================\n")

    async def run(self):
        """Run the API testing loop."""
        print(f"Starting API test against {self.base_url}")
        print(f"Interval: {self.interval} seconds")
        if self.max_requests:
            print(f"Will make {self.max_requests} requests")
        print("Press Ctrl+C to stop\n")

        self.start_time = time.time()

        async with aiohttp.ClientSession() as session:
            while True:
                if self.max_requests and self.request_count >= self.max_requests:
                    break

                self.request_count += 1
                result = await self.make_request(session)

                # Print result details
                status = result["status"]
                elapsed = result["elapsed"]
                timestamp = result["timestamp"]

                print(f"Request {self.request_count}: Status={status}, Time={elapsed:.3f}s, Timestamp={timestamp}")

                if self.request_count % 10 == 0:  # Print stats every 10 requests
                    self.print_stats()

                await asyncio.sleep(self.interval)


def main():
    parser = argparse.ArgumentParser(description='API Endpoint Testing Tool')
    parser.add_argument('url', help='Base URL of the API to test')
    parser.add_argument('-i', '--interval', type=float, default=1.0,
                        help='Interval between requests in seconds (default: 1.0)')
    parser.add_argument('-n', '--num-requests', type=int,
                        help='Number of requests to make (default: unlimited)')

    args = parser.parse_args()

    tester = APITester(args.url, args.interval, args.num_requests)

    try:
        asyncio.run(tester.run())
    except KeyboardInterrupt:
        print("\nTest interrupted by user")
    finally:
        tester.print_stats()


if __name__ == "__main__":
    main()