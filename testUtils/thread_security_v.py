import requests
import concurrent.futures
from datetime import datetime

# 目标 URL
url = "http://127.0.0.1:8000/api/hello"

class SecurityTest:
    def __init__(self, url: str, total: int, concurrent_count: int):
        self.url = url
        self.total = total
        self.concurrent_count = concurrent_count
        self.http_errors_num = 0
        self._detail_http_errors = []  # 存储详细的 HTTP 错误信息
        self.results = []
        self.__start_time = None  # 开始时间
        self.__end_time = None  # 结束时间
        self.elapsed_time = None  # 总耗时
        self.req_per_sec = None  # 每秒请求数
        self.error_info = ""  # 错误信息汇总

    def _task(self) -> None:
        try:
            response = requests.get(self.url)
            if response.status_code != 200:  # 如果 HTTP 状态码不是 200，记录为错误
                self.http_errors_num += 1
                error_result = {"code": response.status_code, "url": self.url, "error": response.text}
                self._detail_http_errors.append(error_result)
        except Exception as e:  # 捕获网络请求中的异常
            self.http_errors_num += 1
            error_result = {"code": "unknown", "url": self.url, "error": str(e)}
            self._detail_http_errors.append(error_result)

    def _test_loop(self) -> None:
        # 记录测试开始时间
        self.__start_time = datetime.now()
        with concurrent.futures.ThreadPoolExecutor(max_workers=self.concurrent_count) as executor:
            # 提交任务到线程池
            futures = [executor.submit(self._task) for _ in range(self.total)]
            # 等待所有任务完成
            concurrent.futures.wait(futures)
        
        # 记录测试结束时间
        self.__end_time = datetime.now()
        # 计算总耗时
        self.elapsed_time = self.__end_time - self.__start_time
        # 计算每秒请求数
        self.req_per_sec = self.total / self.elapsed_time.total_seconds()
        # 格式化错误信息
        self.error_info = self._format_detail_http_errors()

    def _format_detail_http_errors(self) -> str:
        # 根据 HTTP 错误码分类，格式化错误信息
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

    def run(self) -> None:
        self._test_loop()

    def get_results(self) -> str:
        result = ""
        result += f"总请求数: {self.total}\n"
        result += f"并发数: {self.concurrent_count}\n"
        result += f"HTTP 错误数: {self.http_errors_num}\n"
        result += f"详细 HTTP 错误信息: \n{self.error_info}\n"
        result += f"总耗时: {self.elapsed_time}\n"
        result += f"每秒请求数: {self.req_per_sec:.2f}\n"
        return result

if __name__ == "__main__":
    test = SecurityTest(url, 1000, 100)
    test.run()
    print(test.get_results())