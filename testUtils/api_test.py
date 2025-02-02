
import requests

BASE = "http://127.0.0.1:8000/api/"

USER = f"{BASE}user"
LOGIN = f"{USER}/login"
REG = f"{USER}/register"
DELETE = f"{USER}/delete/{{id}}"
CHANGE = f"{USER}/change"

TOKEN = r"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyIjoidGVzdFVzZXIiLCJuYmYiOjE3Mzg1MDAxNTIsImV4cCI6MTczOTEwNDk1MiwiaWF0IjoxNzM4NTAwMTUyLCJpc3MiOiJob2JieS1ncm91cCJ9.rfaGLg8s3K1f7jJ11JghrXJ1CXZnM9XKOYmBd01wG3k"

HEADERS = {
    "Authorization": f"Bearer {TOKEN}"
}

reg_data = {
    "username": "testUser",
    "email": "test@email.com",
    "password": "password123"
}

login_data = {
    "username": "testUser",
    "password": "password123"
}

change_pwd = {
    "username": "testUser",
    "OldPassword": "password123",
    "NewPassword": "password456"
}

def test_case(action: str = "register") -> None:
    # Register
    match action:
        case "register":
            response = requests.post(REG, json=reg_data)
        case "login":
            response = requests.post(LOGIN, json=login_data)
        case "change":
            response = requests.post(CHANGE, json=change_pwd)
        case "delete":
            response = requests.delete(DELETE.format(id=1))
        case _:
            print("Invalid action")
            return
        
    print(response.status_code)
    print(response.text)

if __name__ == '__main__':
    test_case("delete")