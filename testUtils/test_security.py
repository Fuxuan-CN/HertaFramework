import asyncio
import aiohttp
from datetime import datetime

# 目标 URL
url = "http://127.0.0.1:8000/api/hello"

class SecurityTest:
    def __init__(self, url: str, total: int, concurrent_count: int):
        self.url = url
        self.total = total
        self.concurrent_count = concurrent_count
        self.http_errors_num = 0
        self._detail_http_errors = []
        self.results = []
        self.__start_time = None
        self.__end_time = None
        self.elapsed_time = None
        self.req_per_sec = None
        self.error_info = ""

    async def _task(self, session):
        try:
            async with session.get(self.url) as response:
                if response.status != 200:
                    self.http_errors_num += 1
                    error_result = {"code": response.status, "url": self.url, "error": await response.text()}
                    self._detail_http_errors.append(error_result)
        except Exception as e:
            self.http_errors_num += 1
            error_result = {"code": "unknown", "url": self.url, "error": str(e)}
            self._detail_http_errors.append(error_result)

    async def _test_loop(self):
        self.__start_time = datetime.now()
        async with aiohttp.ClientSession() as session:
            tasks = [self._task(session) for _ in range(self.total)]
            await asyncio.gather(*tasks)
        self.__end_time = datetime.now()
        self.elapsed_time = self.__end_time - self.__start_time
        self.req_per_sec = self.total / self.elapsed_time.total_seconds()
        self.error_info = self._format_detail_http_errors()

    def _format_detail_http_errors(self):
        error_count = {}
        for error in self._detail_http_errors:
            code = error["code"]
            msg = error["error"]
            if code not in error_count:
                error_count[code] = {"count": 0, "msg": msg}
            error_count[code]["count"] += 1

        error_info = ""
        for code, info in error_count.items():
            error_info += f"HTTP 状态码 {code} 出现 {info['count']} 次，错误信息: {info['msg']}\n"
        return error_info

    def run(self):
        asyncio.run(self._test_loop())

    def get_results(self):
        result = ""
        result += f"总请求数: {self.total}\n"
        result += f"并发数: {self.concurrent_count}\n"
        result += f"HTTP 错误数: {self.http_errors_num}\n"
        result += f"详细 HTTP 错误信息: \n{self.error_info}\n"
        result += f"总耗时: {self.elapsed_time}\n"
        result += f"每秒请求数: {self.req_per_sec:.2f}\n"
        return result

if __name__ == "__main__":
    test = SecurityTest(url, total=1000, concurrent_count=100)
    test.run()
    print(test.get_results())