import socket
import json
import os
import select
import threading
from queue import Queue
from time import sleep
import json

class USE_Client:
    def __init__(self, debug=False, PORT):

        self.debug = debug
        self.buffer_size=4096
        self.PORT = PORT

        # create an ipv4 (AF_INET) socket object using the tcp protocol (SOCK_STREAM)
        self.client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

        # connect the client
        # client.connect((target, port))
        self.client.connect(('127.0.0.1', self.PORT))
        self.flag_stop_listening = threading.Event()

        self.socket_rcv_calls = Queue()

    def _rcv_data(self, buffer_size=4096):
        self.socket_rcv_calls.put(1)
        # receive the response data (4096 is recommended buffer size)
        response = self.client.recv(buffer_size)
        self.socket_rcv_calls.get()
        return response

    def _listen(self, callback):
        if self.debug:
            print("listening...")
        while not self.flag_stop_listening.is_set():
            ready_to_read, ready_to_write, in_error = select.select([self.client], [], [], 0.1)
            for s in ready_to_read:
                if self.socket_rcv_calls.empty():
                    asyncData = json.loads(s.recv(self.buffer_size))
                    if self.debug:
                        print("listening returned:" + str(asyncData))
                    callback(asyncData)
            sleep(.1)

    def reset(self, callbackAbortTrial, use_screenshot=False, screenshot_path='screenshot.jpg'):
        if self.debug:
            print('reset()')
        # if the game was running on the server, and the previous trial was aborted, but the user did not send ACK, then make sure the server gets and ACK and does not wait for abort_trial_ack
        self.ack_abort_trial()
        data = {
            'USE_SCREENSHOT': use_screenshot,
            'SCREENSHOT_PATH': os.path.abspath(screenshot_path)
        }
        req = {
            'CMD': 'RESET',
            'DATA_TYPE': 'RESET_DATA',
            'DATA': json.dumps(data)
        }
        reqs = json.dumps(req)
        self.client.send(reqs.encode('utf-8'))

        response = self._rcv_data(self.buffer_size)
        if self.debug:
            print(response)

        self.thread_listen = threading.Thread(target=self._listen, args=(callbackAbortTrial,))
        self.thread_listen.setDaemon(True)
        self.thread_listen.start()

    def get_action_size(self):
        if self.debug:
            print("get_action_size()")
        req = {
            'CMD': 'GET_ACTION_SIZE',
            'DATA_TYPE': '',
            'DATA': ''
        }
        reqs = json.dumps(req)
        self.client.send(reqs.encode('utf-8'))

        response = int(self._rcv_data(self.buffer_size))
        if self.debug:
            print(response)

        return response

    def step(self):
        if self.debug:
            print('\n\nstep()')
        req = {
            'CMD': 'STEP',
            'DATA_TYPE': '',
            'DATA': ''
        }
        reqs = json.dumps(req)
        self.client.send(reqs.encode('utf-8'))

        response = json.loads(self._rcv_data(self.buffer_size))
        if self.debug:
            print(response)
        return response

    def act(self, action):
        if self.debug:
            print('act()')
        self.flag_stop_listening.set()
        req = {
            'CMD': 'ACT',
            'DATA_TYPE': 'INTEGER_ACTION',
            'DATA': str(action) + ''
        }
        reqs = json.dumps(req)
        self.client.send(reqs.encode('utf-8'))

        response = json.loads(self._rcv_data(self.buffer_size))
        if self.debug:
            print(response)
        self.flag_stop_listening.clear()
        if 'CMD' in response:
            if self.debug:
                print("trial aborted")
            self.ack_abort_trial()
            return None
        return response

    def ack_abort_trial(self):
        sleep(.3)
        if self.debug:
            print('ack_abort_trial()')
        req = {
            'CMD': 'ACK_ABORT_TRIAL',
            'DATA_TYPE': '',
            'DATA': ''
        }
        reqs = json.dumps(req)
        self.client.send(reqs.encode('utf-8'))

        response = str(self._rcv_data(self.buffer_size))
        if self.debug:
            print(response)
        return response

    def close(self):
        self.flag_stop_listening.set()
        self.client.close()