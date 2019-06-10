# Chatter

Chat with your friend freely on the Intranet!

## Warnings

- the safety of the message is based on the safety of your own computer
- sending plain text has no safety
- don't expose your private key to anyone else
- keys that outputted as files are not encrypted
- all credential message are stored in the memory when the program is running
- ~~if man in the middle both got your public key and spoofed the opposite IP, he can fake his identification as the opposite~~
- It is your job to check if other's public key is authentic
- make sure that the signal "encrypted" is shown before sending credential messages.
- if others sniff the traffic, the message can be cracked theoretically by brute force though it might take a long time
- Chances are, though few, the unknown defect of the overall encryption logic or of the cryptography might be a potential security threat.

## How to use

- Specify the IP address of the **server's**
- Pick a proper role as either a server or a client
- If you've not saved the key pair yet
  - save the key pair
- If you've saved key pair
  - load up your own private key
    - you can also drop the file on the window
- Send public key to your friend in any channel if not 've done
- Double check the authenticity of the incoming public key from your friend with him / her
- Load up **other's** public key
  - you can also drop the file on the window
- Randomly type some key to the text box at the bottom of the window. Better more.
- Wait until your friend has successfully connected to your app
- Click "Standby" or "Send Session Key!"
- Chat with your friend freely!

## File Structure

Class

- "MainWindow.xaml.vb" - main entry of the program
  - Task-bar flashing
- "ChatBox.xaml.vb" - code behind UI for chat interface
- "CryptoPanel.xaml.vb" - code behind UI for cryptography interface
  - File drop
- "AesApi.vb" - providing AES
- "RsaApi.vb" - providing RSA
- "SocketBase.vb" - handling all logic of socket operations
- "SocketClient.vb" - handling client initiation - inherited from "SocketBase.vb"
- "SocketListener.vb" - handling server initiation - inherited from "SocketBase.vb"
- "SocketManager.vb" - pending for releasing some code from "SocketBase.vb"
- "FlashWindow.vb" - shared class handling the flashing icon in task-bar

Xaml

- "MainWindow.xaml" - main entry of the program
- "ChatBox.xaml" - UI for chat interface
- "CryptoPanel.xaml" - UI for cryptography interface

## Basic Principles

### Inclusive relationship of classes

- `MainWindow`
  - `CryptoPanel` - init
    - `SocketBase`
  - `ChatBox` - init
  - `SocketClient` - init
    - `SocketBase` - init - base
      - `AesApi` - init
      - `RsaApi` - init
  - `SocketListener` - init
    - `SocketBase` - init - base
      - ...
  - `SocketBase`
  - `FlashWindow` - shared

PS

- If the class B has shown in the code of class A, then class A is said to be the upper class relative to class B.
- the mark "init" indicates that the class with the mark initiates under its upper class
- the mark "base" indicates that the class with the mark is the base class of its upper class
- the mark "shared" indicates that the class with the mark is used only by its shared functions
- "..." indicates that the details have already shown above therefore omitted

### Lifetime of the program

In User-oriented order

- When the program starts, it will require user to choose a role from being a server or being a client.
  - when user has done choosing, the ip address and port of the server and the expected client address are not allowed to be changed.
  - the socket object is then instantiated.
    - if the user choose to be server, the class `SocketListener` will be instantiated.
      - `SocketListener` is derived from `SocketBase`
      - `SocketListener` only implements the initiation of a socket.
        - Stopped when done connection
    - if the user choose to be client, the class `SocketClient` will be instantiated
      - `SocketClient` is derived from `SocketBase`
      - `SocketClient` only implements the initiation of a socket.
        - Stopped when done connection
    - `SocketBase` deals with the lifetime of a socket after its connection
- User then is needed to set up the **private key** and the **public key** properly.
  - Conducted in class `SocketBase`
  - public key should belong to the peer that the user wants to connect to.
  - private key should belong to the user himself or herself
  - those key pair is used to encrypt the session key
  - the key pair here has been automatically generated through pseudo-randomization in the instantiation of the class `RsaApi`
    - the class `RsaApi` has already instantiated in the constructor of `SocketBase`
  - after setting up public key, the user can send "stand-by" signal to his peer.
    - the act will give the right for his peer to generate a session key
- Then the user can send "stand-by" signal to his peer.
  - Conducted in class `SocketBase`
  - the act will give the right for his peer to generate a session key
- If the peer send in the "stand-by" signal, the user who received it should generate a session key.
  - Conducted in class `SocketBase`
  - session key generation is conducted in class `AesApi`
    - class `AesApi` has been instantiated in `SocketBase` during the handshakes
  - the details are illustrated below.
- Who generating the session key launches the three-way handshake
  - Conducted in class `SocketBase`
  - the handshakes are aimed to exam the authenticity of both his peer and himself and to send the session key to the one who has sended "stand-by" signal.
  - the details are illustrated below.
- When the implementation done, the user and his peer can now chat in the chat box
  - the login panel will collapsed and the chat panel will be visible.
    - login panel includes the user control implemented in "CryptoPanel.xaml" and part of controls implemented in "MainWindow.xaml"
    - chat panel is implemented in another user control "ChatBox.xaml"
  - there are the receiving and sending processes
    - Conducted in class `SocketBase`
    - the receiving process is run in looping thread

### Introduction to Socket

Almost all implementation is in the class `SocketBase` which resides in "SocketBase.vb"

#### Message Framing

"There is no way to send a packet of data over TCP; that function call does not exist. Rather, there are two streams in a TCP connection: an incoming stream and an outgoing stream. One may read from the incoming stream by calling a "receive" method, and one may write to the outgoing stream by calling a "send" method. If one side calls "send" to send 5 bytes, and then calls "send" to send 5 more bytes, then there are 10 bytes that are placed in the outgoing stream. The receiving side may decide to read them one at a time from its receiving stream if it so wishes (calling "receive" 10 times), or it may wait for all 10 bytes to arrive and then read them all at once with a single call to "receive"."

ref - <https://www.codeproject.com/Articles/37496/TCP-IP-Protocol-Design-Message-Framing>

#### ...

TODO...

### Encryption

Basic steps are shown in pseudo-code below

#### Three-way handshake

```VB
    Dim sKey As Byte()
    Dim IV As Byte()
    Dim encryptedSKey As Byte()
    Dim encryptedIV As Byte()

    Dim pubKey As String
    ' Dim priKey As String

    Dim plainText As String = "plain text"
    Dim cipherText As Byte()

    Dim RSA1 As New RsaApi()   ' public key receiver
    Dim RSA2 As New RsaApi()   ' private key holder

    Dim AES1 As New AesApi(5330)  ' set seed as `5330`, which should be set randomly by yourself
    Dim AES2 As New AesApi(0)  ' the seed here is not necessary, since its session key will be replaced later

    ' 1 sends his own public key to 2 and vice versa
    ' public key exchange (or more precisely, exchange public key)
    ' This step should be done through other message channel, which can be unsafe. However, you MUST check the authenticity of other's public key
    pubKey = RSA2.GetMyPublicKey()
    RSA1.SetOthersPublicKey(pubKey)
    pubKey = RSA1.GetMyPublicKey()
    RSA2.SetOthersPublicKey(pubKey)

    '''''''''''''''''Begin handshake for three times''''''''''''''''''''

    '''''''first time'''''''''
    ' 1 takes out his session key
    sKey = AES1.GetSessionKey()
    IV = AES1.GetIV()

    ' 1 encrypts session key
    encryptedSKey = RSA1.EncryptMsg(sKey + 1)
    encryptedIV = RSA1.EncryptMsg(IV + 1)

    ' 1 sends the encrypted session key through this app

    ' 2 received it

    ' 2 decrypt it and get the session key
    sKey = RSA2.DecryptMsg(encryptedSKey) - 1
    IV = RSA2.DecryptMsg(encryptedIV) - 1

    ' 2 sets the session key as his own session key
    AES2.SetSessionKey(sKey)
    AES2.SetIV(IV)

    ''''''''''Second time''''''''''''''
    ' 2 takes out his session key
    sKey = AES2.GetSessionKey()
    IV = AES2.GetIV()

    ' 2 encrypts session key
    encryptedSKey = RSA2.EncryptMsg(sKey + 2)
    encryptedIV = RSA2.EncryptMsg(IV + 2)

    ' 2 sends the encrypted session key through this app

    ' 1 received it

    ' 1 decrypt it and get the session key
    sKey = RSA1.DecryptMsg(encryptedSKey) - 2
    IV = RSA1.DecryptMsg(encryptedIV) - 2

    ''Difference here
    ' 1 compares the session key with his own key
    If Not sKey.SequenceEqual(_AES.GetSessionKey()) Then Error()
    If Not IV.SequenceEqual(_AES.GetIV()) Then Error()

    '''''''''Third handshake'''''''''
    ' 1 takes out his session key
    sKey = AES1.GetSessionKey()
    IV = AES1.GetIV()

    ' 1 encrypts session key
    encryptedSKey = RSA1.EncryptMsg(sKey + 3)
    encryptedIV = RSA1.EncryptMsg(IV + 3)

    ' 1 sends the encrypted session key through this app

    ' 2 received it

    ' 2 decrypt it and get the session key
    sKey = RSA2.DecryptMsg(encryptedSKey) - 3
    IV = RSA2.DecryptMsg(encryptedIV) - 3

    ' 2 compares the session key with his own key
    If Not sKey.SequenceEqual(_AES.GetSessionKey()) Then Error()
    If Not IV.SequenceEqual(_AES.GetIV()) Then Error()
    '''''''''''''''''End handshake for three times''''''''''''''''''''
```

#### Sessional Chat

```VB
    ''''''''''''''''Begin Chat''''''''''''''''''

    ' whoever (here is 1) encrypts message by session key
    cipherText = AES1.EncryptMsg(plainText)

    ' updates new IV
    newIv = AES1.GetNewIv()
    AES1.SetIv(newIv)

    ' prepends IV to cipherText
    aesPacket = __CONCAT(newIv, cipherText)

    ' whoever (here is 1) sends

    ' the opposite (here is 2) received

    ' splits IV and cipherText out
    IV = aesPacket.__FirstPart
    cipherText = aesPacket.__LastPart

    ' updates IV
    IV = AES2.SetIv(IV)

    ' the opposite (here is 2) decrypt message by session key
    plainText = AES2.DecryptMsg(cipherText)
```

#### Session Key

Session key is a key for AES, which is created randomly.
The randomization is both human-based and time-based, whose byte's representations are merged through XOR operation.
At last, the random byte array will be processed by SHA256 hashing.
The final hashing return will be the session key.

### Message Format

Hierarchy of Message

- `MessagePackage` (etc. Message)
  - `AesContentPackage` (etc. Content) - content that is encrypted thru AES

`MessagePackage`

- `Kind`
  - `PlaintextSignal`
  - `Cipher`
  - `Plaintext`
  - `EncryptedSessionKey`
- `Content` - Depends on what specific kind of message is

`Content` type

- `Enum` - For PlaintextSignal
- `Byte()` - For Cipher and EncryptedSessionKey - Byte-ization of `AesContentPackage`
- `Text` - For Plaintext

`AesContentPackage`

- `Kind`
  - `Text`
  - `Image`  ' TODO
  - `Feedback`

TODO: explain different derived class under `AesContentPackage`

#### Cipher Content

- Denote arbitrary integer as `[int]`, arbitrary text as `[text]`

- plain text - `{"$type":"SocketTest.AesTextPackage, SocketTest","MessageID":[int],"Text":"[text]","Kind":0}`

- cipher content - denoted as `[c]`, which is stored in byte array `As Byte()`, encrypted from `AesContentPackage`

- IV - denoted as `[IV]`, which is stored in byte array `As Byte()`

- send-out package - `{"$type":"SocketTest.CipherMessagePackage, SocketTest","Content":{"$type":"System.Byte[], mscorlib","$value":"[IV][c]"},"Kind":1}`

#### Plain-text message

- plaintext - JSON expression unknown

#### Feedback message

- Denote arbitrary integer as `[int]`

- plaintext - `{"$type":"SocketTest.AesFeedbackPackage, SocketTest","MessageID":[int],"Kind":2}`

- remaining process goes to "cipher Content" part

- encrypted content denotes as `[c]`

- `{"$type":"SocketTest.CipherMessagePackage, SocketTest","Content":{"$type":"System.Byte[], mscorlib","$value":"[c]"},"Kind":1}`

#### Standby message

- `{"$type":"SocketTest.PlaintextSignalMessagePackage, SocketTest","Content":0,"Kind":0}`

#### Handshake message

- encrypted session key - stored in byte array `As Byte()` - denote as `[sK]`

- `{"$type":"SocketTest.EncryptedSessionKeyMessagePackage, SocketTest","Content":{"$type":"System.Byte[], mscorlib","$value":"[sK]"},"Kind":3}`

### Feedback system

Procedure (Suppose the previous message ID from *A* denotes `aID'`)

- sender *A* sends a message with ID `aID`, which is the same as `aID' + 1`
- receiver *B* received the message with ID `aID`
- *B* verifies `aID` with the previous message ID `aID'` from *A*
  - expected equation - `aID' + 1 == aID`
- receiver *B* sends a feedback to *A* with ID `aID`
- *B* displays the message on screen
- *A* receives the feedback with ID `aID`
- *A* displays the message on screen

## Known BUG

- The thread might fail to exit. Check your process manually to make sure the program has exited

## Known Limitation

- each transmission allows message only up to 1024 bit

## Acknowledgement

- Authors whose code original on the website is used in this project. Those links of webs were written among code.

## TODO

- [x] encrypt the message
- [x] check if the opposite received the key
- [x] check if the opposite received the message
- [ ] break the limit of the length of each message
- [ ] open once, connect many times
- [x] server bans illegal connections and still keeps alive listening
- [ ] change session key during the same session
- [ ] reduce the three-time handshakes to just once
- [x] change IV for each message. [IV can be made public](https://crypto.stackexchange.com/questions/3965/what-is-the-main-difference-between-a-key-an-iv-and-a-nonce)
- [x] salting session key
- [ ] Scan peers on the same Intranet
- [ ] write documentations for each class
- [x] Ensure integrity of each message in Application Layer assuming no package loss
- [ ] Deal with package loss
