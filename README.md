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
- If 've not saved key pair yet
  - save key pair
- If 've saved key pair
  - load up your own private key
- Send public key to your friend in any channel if not 've done
- Double check the authenticity of the incoming public key with your friend
- Load up **other's** public key
- Randomly type some key to the text box at the bottom of the window. Better more.
- Wait until your friend has successfully connected to your app
- Click "Standby" or "Send Session Key!"
- Chat with your friend freely!

## File Structure

Class

- "MainWindow.xaml.vb" - main entry of the program
- "ChatBox.xaml.vb" - code behind UI for chat interface
- "CryptoPanel.xaml.vb" - code behind UI for cryptography interface
- "AesApi.vb" - providing AES
- "RsaApi.vb" - providing RSA
- "SocketBase.vb" - handling all logics of socket operations
- "SocketClient.vb" - handling client initiation - inherited from "SocketBase.vb"
- "SocketListener.vb" - handling server initiation - inherited from "SocketBase.vb"
- "SocketManager.vb" - pending for releasing some code from "SocketBase.vb"

Xaml

- "MainWindow.xaml" - main entry of the program
- "ChatBox.xaml" - UI for chat interface
- "CryptoPanel.xaml" - UI for cryptography interface

## Basic Principles

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

### Message Format

#### Cipher message

- plain text - `<ID>112</ID><TEXT>this is a text</TEXT><EOF/>`

- cipher text - denoted as `[c]`, which is stored in byte array `As Byte()`, encrypted from the plain text

- IV - denoted as `[IV]`, which is stored in byte array `As Byte()`

- send-out package - `[IV][c]`

#### Plain-text message

- plain text - `<ID>112</ID><TEXT>this is a text</TEXT><EOF/>`

#### Feedback message

- plain text - `<ID>112</ID><FB/><EOF/>`

- remaining process goes to "cipher message" part

#### Standby message

- `<STANDBY/><EOF/>`

#### Handshake message

- encrypted session key - stored in byte array `As Byte()`

### Feedback system

Procedure

- sender *A* sends a message
- receiver *B* received the message
- receiver *B* sends a feedback to *A*
- *B* displays the message on screen
- *A* receives the feedback
- *A* displays the message on screen

## Known BUG

- The thread might fail to exit. Check your process manually to make sure the program has exited

## Acknowledgement

- Authors whose original code is at the website whose links were written between my codes.

## TODO

- [x] encrypt the message
- [x] check if the opposite received the key
- [x] check if the opposite received the message
- [ ] break the limit of the length of each message
- [ ] open once, connect many times
- [x] server bans illegal connections and still keeps alive listening
- [ ] change session key during the same session
- [ ] ~~allow customizing the generation of IV of AES~~
- [ ] reduce the three-time handshakes to just once
- [x] change IV for each message. [IV can be made public](https://crypto.stackexchange.com/questions/3965/what-is-the-main-difference-between-a-key-an-iv-and-a-nonce)
