import requests
import concurrent.futures
import time

# 目标 URL
url = "http://127.0.0.1:8000/api/hello"

# 请求总数
total_requests = 1000

# 并发数
concurrent_requests = 100

# 记录成功和失败的请求
success_count = 0
failure_count = 0

def send_request():
    global success_count, failure_count
    try:
        response = requests.get(url)
        if response.status_code == 200:
            success_count += 1
        else:
            failure_count += 1
        print(f"Request {response.status_code}: {response.text}")
    except Exception as e:
        print(f"Request failed: {e}")
        failure_count += 1

if __name__ == "__main__":
    start_time = time.time()

    # 使用线程池发送请求
    with concurrent.futures.ThreadPoolExecutor(max_workers=concurrent_requests) as executor:
        futures = [executor.submit(send_request) for _ in range(total_requests)]

    end_time = time.time()

    print(f"\nTotal Requests: {total_requests}")
    print(f"Successful Requests: {success_count}")
    print(f"Failed Requests: {failure_count}")
    print(f"Total Time: {end_time - start_time:.2f} seconds")
    print(f"Requests per second: {total_requests / (end_time - start_time):.2f}")