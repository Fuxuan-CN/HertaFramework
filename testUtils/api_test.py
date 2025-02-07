
import requests

BASE = "http://127.0.0.1:8000/api"

USER = f"{BASE}/user"
LOGIN = f"{USER}/login"
REG = f"{USER}/register"
DELETE = f"{USER}/delete"
CHANGE = f"{USER}/change"
UPDATE_INFO = f"{USER}/update/info"
GET_INFO = f"{USER}/get/info/{{id}}"

TOKEN = r"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyIjoidGVzdFVzZXIiLCJ1c2VySWQiOiIxIiwibmJmIjoxNzM4ODk4ODQ4LCJleHAiOjE3Mzk1MDM2NDgsImlhdCI6MTczODg5ODg0OCwiaXNzIjoiaG9iYnktZ3JvdXAifQ.F907K6OG7fY2uTwNh10GnPBaWML2UK1p6xU-to5w6pIeyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VyIjoidGVzdFVzZXIiLCJ1c2VySWQiOiIxIiwibmJmIjoxNzM4ODk4ODQ4LCJleHAiOjE3Mzk1MDM2NDgsImlhdCI6MTczODg5ODg0OCwiaXNzIjoiaG9iYnktZ3JvdXAifQ.F907K6OG7fY2uTwNh10GnPBaWML2UK1p6xU-to5w6pI"

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
    "username": "testUser2",
    "OldPassword": "password123",
    "NewPassword": "password456"
}

update_patch = {
    "UserId": 1,
    "updateInfo": {
        "Nickname": "testUser2",
        "Hobbies": ["reading", "swimming"]
    }
}

delete_user = {
    "Username": "testUser",
    "Password": "password123",
    "Reason": "testing"
}

def test_user(action: str = "register") -> None:
    # Register
    match action:
        case "register":
            response = requests.post(REG, json=reg_data)
        case "login":
            response = requests.post(LOGIN, json=login_data)
        case "change":
            response = requests.post(CHANGE, json=change_pwd, headers=HEADERS)
        case "delete":
            response = requests.delete(DELETE, json=delete_user, headers=HEADERS)
        case "update_info":
            response = requests.patch(UPDATE_INFO, json=update_patch, headers=HEADERS)
        case "get_info":
            response = requests.get(GET_INFO.format(id=1))
        case _:
            print("Invalid action")
            return
        
    print(response.status_code)
    print(response.text)

if __name__ == '__main__':
    test_user("delete")