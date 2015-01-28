using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Maarg.AllAboard;
using Maarg.Contracts;
using Maarg.Fatpipe.LoggingService;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace Maarg.FatpipeAPI
{

    class FatpipeMessage : IFatpipeMessage, IMessageHandlerAgent, IOutboundFatpipeMessage // - use the DALImplementation Class member variable
    {
        MessageHeader _header;
        MessageBody _body;
        MessageStatus _status;

        AgentType _agentType;
        OperationType _operationType;
        string _roleName;
        string _roleInstanceId;
        string _ServerName;
        string _serviceAddress;
        FatpipeManager fpm;
        RoutingInfo routingInfo;


        public FatpipeMessage(string tenantIdentifier, string msgId, FatpipeManager fpm)
        {

            _header = new MessageHeader(tenantIdentifier, msgId);
            _body = new MessageBody();
            _status = new MessageStatus();
            this.fpm = fpm;
            routingInfo = null;

        }

        #region Properties
        public RoutingInfo RoutingInfo
        {
            get { return this.routingInfo; }
            set { this.routingInfo = value; }
        }

        public MessageHeader Header
        {
            get
            {
                return _header;
            }
            set
            {
                _header = value;
            }
        }

        public MessageBody Body
        {
            get
            {
                return _body;
            }
            set
            {
                _body = value;
            }
        }

        public MessageStatus Status
        {
            get
            {
                return _status;
            }
            set
            {
                _status = value;
            }
        }
        #endregion

        #region implementing IMessageHandler


        public AgentType AgentType
        {
            get
            {
                return _agentType;
            }
            set
            {
                _agentType = value;
            }
        }


        public OperationType OperationType
        {
            get
            {
                return _operationType;
            }
            set
            {
                _operationType = value;
            }
        }

        public string RoleName
        {
            get
            {
                return _roleName;

            }
            set
            {
                _roleName = value;
            }
        }

        public string RoleInstanceId
        {
            get
            {
                return _roleInstanceId;
            }

            set
            {
                _roleInstanceId = value;
            }
        }

        public string ServerName
        {
            get
            {
                return _ServerName;

            }
            set
            {
                _ServerName = value;
            }
        }


        public string ServiceAddress
        {
            get
            {
                return _serviceAddress;
            }
            set
            {
                _serviceAddress = value;
            }
        }


        #endregion


        public IFatpipeMessage Clone(Stream bodyStream)
        {
            IFatpipeMessage msg = this.fpm.CreateNewMessage();
            msg.Body.Body = bodyStream;
            return msg;
        }


    }
}
