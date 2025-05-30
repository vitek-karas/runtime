﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.Diagnostics.Tracing;
using System.Net.Sockets;
using System.Reflection;
using Microsoft.Quic;

namespace System.Net.Quic.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    public abstract class QuicTestBase : IDisposable
    {
        public const long DefaultStreamErrorCodeClient = 123456;
        public const long DefaultStreamErrorCodeServer = 654321;
        public const long DefaultCloseErrorCodeClient = 789;
        public const long DefaultCloseErrorCodeServer = 987;

        private static readonly byte[] s_ping = "PING"u8.ToArray();
        private static readonly byte[] s_pong = "PONG"u8.ToArray();

        public static bool IsSupported => QuicListener.IsSupported && QuicConnection.IsSupported;
        public static bool IsNotArm32CoreClrStressTest => !(CoreClrConfigurationDetection.IsStressTest && PlatformDetection.IsArmProcess);

        public static bool IsIPv6Available => Configuration.Sockets.IsIPv6LoopbackAvailable;

        public static SslApplicationProtocol ApplicationProtocol { get; } = new SslApplicationProtocol("quictest");

        public readonly X509Certificate2 ServerCertificate = System.Net.Test.Common.Configuration.Certificates.GetServerCertificate();
        public readonly X509Certificate2 ClientCertificate = System.Net.Test.Common.Configuration.Certificates.GetClientCertificate();

        public ITestOutputHelper _output;
        public const int PassingTestTimeoutMilliseconds = 4 * 60 * 1000;
        public static TimeSpan PassingTestTimeout => TimeSpan.FromMilliseconds(PassingTestTimeoutMilliseconds);

        static QuicTestBase()
        {
            // Opt in to run with OpenSSL version on MsQuic on older windows where Schannel
            // version is not supported.
            //
            // This has to happen here in order to be called before QuicTestBase.IsSupported
            if (PlatformDetection.IsWindows10OrLater && !QuicTestCollection.IsWindowsVersionWithSchannelSupport())
            {
                AppContext.SetSwitch("System.Net.Quic.AppLocalMsQuic", true);
            }
        }

        public QuicTestBase(ITestOutputHelper output)
        {
            _output = output;
        }

        public void Dispose()
        {
            ServerCertificate.Dispose();
            ClientCertificate.Dispose();
        }

        public bool RemoteCertificateValidationCallback(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
        {
            Assert.Equal(ServerCertificate.GetCertHash(), certificate?.GetCertHash());
            return true;
        }

        public async Task<QuicException> AssertThrowsQuicExceptionAsync(QuicError expectedError, Func<Task> testCode)
        {
            QuicException ex = await Assert.ThrowsAsync<QuicException>(testCode);
            _output.WriteLine(ex.ToString());
            Assert.Equal(expectedError, ex.QuicError);
            return ex;
        }

        public QuicServerConnectionOptions CreateQuicServerOptions()
        {
            return new QuicServerConnectionOptions()
            {
                DefaultStreamErrorCode = DefaultStreamErrorCodeServer,
                DefaultCloseErrorCode = DefaultCloseErrorCodeServer,
                ServerAuthenticationOptions = GetSslServerAuthenticationOptions()
            };
        }

        public SslServerAuthenticationOptions GetSslServerAuthenticationOptions()
        {
            return new SslServerAuthenticationOptions()
            {
                ApplicationProtocols = new List<SslApplicationProtocol>() { ApplicationProtocol },
                ServerCertificate = ServerCertificate
            };
        }

        public SslClientAuthenticationOptions GetSslClientAuthenticationOptions(string targetHost = "localhost")
        {
            return new SslClientAuthenticationOptions()
            {
                ApplicationProtocols = new List<SslApplicationProtocol>() { ApplicationProtocol },
                RemoteCertificateValidationCallback = RemoteCertificateValidationCallback,
                TargetHost = targetHost
            };
        }

        public QuicClientConnectionOptions CreateQuicClientOptions(EndPoint endpoint)
        {
            return new QuicClientConnectionOptions()
            {
                DefaultStreamErrorCode = DefaultStreamErrorCodeClient,
                DefaultCloseErrorCode = DefaultCloseErrorCodeClient,
                RemoteEndPoint = endpoint,
                ClientAuthenticationOptions = GetSslClientAuthenticationOptions()
            };
        }

        internal ValueTask<QuicConnection> CreateQuicConnection(IPEndPoint endpoint)
        {
            var options = CreateQuicClientOptions(endpoint);
            return CreateQuicConnection(options);
        }

        internal ValueTask<QuicConnection> CreateQuicConnection(QuicClientConnectionOptions clientOptions)
        {
            return QuicConnection.ConnectAsync(clientOptions);
        }

        internal QuicListenerOptions CreateQuicListenerOptions(IPAddress address = null, Action<QuicServerConnectionOptions> changeServerOptions = null)
        {
            address ??= IPAddress.Loopback;
            return new QuicListenerOptions()
            {
                ListenEndPoint = new IPEndPoint(address, 0),
                ApplicationProtocols = new List<SslApplicationProtocol>() { ApplicationProtocol },
                ConnectionOptionsCallback = (_, _, _) =>
                {
                    var options = CreateQuicServerOptions();
                    if (changeServerOptions is not null)
                    {
                        changeServerOptions(options);
                    }
                    return ValueTask.FromResult(options);
                }
            };
        }

        internal ValueTask<QuicListener> CreateQuicListener(IPAddress address = null, Action<QuicServerConnectionOptions> changeServerOptions = null)
        {
            var options = CreateQuicListenerOptions(address, changeServerOptions);
            return CreateQuicListener(options);
        }

        internal ValueTask<QuicListener> CreateQuicListener(IPEndPoint endpoint, Action<QuicServerConnectionOptions> changeServerOptions = null)
        {
            var options = new QuicListenerOptions()
            {
                ListenEndPoint = endpoint,
                ApplicationProtocols = new List<SslApplicationProtocol>() { ApplicationProtocol },
                ConnectionOptionsCallback = (_, _, _) =>
                {
                    var options = CreateQuicServerOptions();
                    if (changeServerOptions is not null)
                    {
                        changeServerOptions(options);
                    }
                    return ValueTask.FromResult(options);
                }
            };
            return CreateQuicListener(options);
        }

        internal ValueTask<QuicListener> CreateQuicListener(QuicListenerOptions options) => QuicListener.ListenAsync(options);

        internal Task<(QuicConnection, QuicConnection)> CreateConnectedQuicConnection(QuicListener listener) => CreateConnectedQuicConnection(null, listener);
        internal async Task<(QuicConnection, QuicConnection)> CreateConnectedQuicConnection(QuicClientConnectionOptions? clientOptions, QuicListenerOptions listenerOptions)
        {
            await using (QuicListener listener = await CreateQuicListener(listenerOptions))
            {
                clientOptions ??= CreateQuicClientOptions(listener.LocalEndPoint);
                if (clientOptions.RemoteEndPoint is IPEndPoint iPEndPoint && !iPEndPoint.Equals(listener.LocalEndPoint))
                {
                    clientOptions.RemoteEndPoint = listener.LocalEndPoint;
                }
                return await CreateConnectedQuicConnection(clientOptions, listener);
            }
        }

        internal async Task<(QuicConnection, QuicConnection)> CreateConnectedQuicConnection(QuicClientConnectionOptions? clientOptions = null, QuicListener? listener = null)
        {
            int retry = 3;
            int delay = 25;
            bool disposeListener = false;

            if (listener == null)
            {
                listener = await CreateQuicListener();
                disposeListener = true;
            }

            clientOptions ??= CreateQuicClientOptions(listener.LocalEndPoint);
            if (clientOptions.RemoteEndPoint is IPEndPoint iPEndPoint && !iPEndPoint.Equals(listener.LocalEndPoint))
            {
                clientOptions.RemoteEndPoint = listener.LocalEndPoint;
            }

            QuicConnection clientConnection = null;
            ValueTask<QuicConnection> serverTask = listener.AcceptConnectionAsync();
            try
            {
                while (retry > 0)
                {
                    retry--;
                    try
                    {
                        clientConnection = await CreateQuicConnection(clientOptions).ConfigureAwait(false);
                        break;
                    }
                    catch (QuicException ex) when (ex.HResult == (int)SocketError.ConnectionRefused)
                    {
                        _output.WriteLine($"ConnectAsync to {clientOptions.RemoteEndPoint} failed with {ex.Message}");
                        await Task.Delay(delay);
                        delay *= 2;

                        if (retry == 0)
                        {
                            Debug.Fail($"ConnectAsync to {clientOptions.RemoteEndPoint} failed with {ex.Message}");
                            throw ex;
                        }
                    }
                }

                QuicConnection serverConnection = await serverTask.ConfigureAwait(false);
                if (disposeListener)
                {
                    await listener.DisposeAsync();
                }

                return (clientConnection, serverConnection);
            }
            catch
            {
                if (clientConnection is not null)
                {
                    await clientConnection.DisposeAsync();
                }
                throw;
            }
        }

        internal async Task PingPong(QuicConnection client, QuicConnection server)
        {
            await using QuicStream clientStream = await client.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
            ValueTask t = clientStream.WriteAsync(s_ping);
            await using QuicStream serverStream = await server.AcceptInboundStreamAsync();

            byte[] buffer = new byte[s_ping.Length];
            int remains = s_ping.Length;
            while (remains > 0)
            {
                int readLength = await serverStream.ReadAsync(buffer, buffer.Length - remains, remains);
                Assert.True(readLength > 0);
                remains -= readLength;
            }
            Assert.Equal(s_ping, buffer);
            await t;

            t = serverStream.WriteAsync(s_pong);
            remains = s_pong.Length;
            while (remains > 0)
            {
                int readLength = await clientStream.ReadAsync(buffer, buffer.Length - remains, remains);
                Assert.True(readLength > 0);
                remains -= readLength;
            }

            Assert.Equal(s_pong, buffer);
            await t;
        }

        internal async Task RunClientServer(Func<QuicConnection, Task> clientFunction, Func<QuicConnection, Task> serverFunction, int iterations = 1, int millisecondsTimeout = PassingTestTimeoutMilliseconds, QuicListenerOptions listenerOptions = null)
        {
            const long ClientCloseErrorCode = 11111;
            const long ServerCloseErrorCode = 22222;

            await using QuicListener listener = await CreateQuicListener(listenerOptions ?? CreateQuicListenerOptions());

            using var serverFinished = new SemaphoreSlim(0);
            using var clientFinished = new SemaphoreSlim(0);

            for (int i = 0; i < iterations; ++i)
            {
                (QuicConnection clientConnection, QuicConnection serverConnection) = await CreateConnectedQuicConnection(listener);
                await using (clientConnection)
                await using (serverConnection)
                {
                    await new[]
                    {
                        Task.Run(async () =>
                        {
                            await serverFunction(serverConnection);
                            serverFinished.Release();
                            await clientFinished.WaitAsync();
                        }),
                        Task.Run(async () =>
                        {
                            await clientFunction(clientConnection);
                            clientFinished.Release();
                            await serverFinished.WaitAsync();
                        })
                    }.WhenAllOrAnyFailed(millisecondsTimeout);
                    try
                    {
                        await serverConnection.CloseAsync(ServerCloseErrorCode);
                    }
                    catch (ObjectDisposedException ex)
                    {
                        _output.WriteLine(ex.ToString());
                    }
                    try
                    {
                        await clientConnection.CloseAsync(ClientCloseErrorCode);
                    }
                    catch (ObjectDisposedException ex)
                    {
                        _output.WriteLine(ex.ToString());
                    }
                }
            }
        }

        internal async Task RunStreamClientServer(Func<QuicStream, Task> clientFunction, Func<QuicStream, Task> serverFunction, bool bidi, int iterations, int millisecondsTimeout)
        {
            byte[] buffer = new byte[1] { 42 };

            await RunClientServer(
                clientFunction: async connection =>
                {
                    await using QuicStream stream = bidi ? await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional) : await connection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional);
                    // Open(Bi|Uni)directionalStream only allocates ID. We will force stream opening
                    // by Writing there and receiving data on the other side.
                    await stream.WriteAsync(buffer);

                    await clientFunction(stream);

                    stream.CompleteWrites();
                },
                serverFunction: async connection =>
                {
                    await using QuicStream stream = await connection.AcceptInboundStreamAsync();
                    Assert.Equal(1, await stream.ReadAsync(buffer));

                    await serverFunction(stream);

                    stream.CompleteWrites();
                },
                iterations,
                millisecondsTimeout
            );
        }

        internal Task RunBidirectionalClientServer(Func<QuicStream, Task> clientFunction, Func<QuicStream, Task> serverFunction, int iterations = 1, int millisecondsTimeout = PassingTestTimeoutMilliseconds)
            => RunStreamClientServer(clientFunction, serverFunction, bidi: true, iterations, millisecondsTimeout);

        internal Task RunUnidirectionalClientServer(Func<QuicStream, Task> clientFunction, Func<QuicStream, Task> serverFunction, int iterations = 1, int millisecondsTimeout = PassingTestTimeoutMilliseconds)
            => RunStreamClientServer(clientFunction, serverFunction, bidi: false, iterations, millisecondsTimeout);

        internal static async Task<int> ReadAll(QuicStream stream, byte[] buffer)
        {
            Memory<byte> memory = buffer;
            int bytesRead = 0;
            while (true)
            {
                int res = await stream.ReadAsync(memory);
                if (res == 0)
                {
                    break;
                }
                bytesRead += res;
                memory = memory[res..];
            }

            return bytesRead;
        }

        internal static async Task<int> WriteForever(QuicStream stream, int size = 1)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                while (true)
                {
                    await stream.WriteAsync(buffer);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
