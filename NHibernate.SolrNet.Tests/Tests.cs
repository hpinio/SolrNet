﻿#region license
// Copyright (c) 2007-2009 Mauricio Scheffer
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
//  
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System;
using System.Collections.Generic;
using MbUnit.Framework;
using NHibernate.Event;
using NHibernate.Tool.hbm2ddl;
using Rhino.Mocks;
using SolrNet;
using Environment=NHibernate.Cfg.Environment;

namespace NHibernate.SolrNet.Tests {
    [TestFixture]
    public class Tests {
        public class SolrNetListener<T> : IPostInsertEventListener, IPostDeleteEventListener, IPostUpdateEventListener {
            private readonly ISolrOperations<T> solr;

            public SolrNetListener(ISolrOperations<T> solr) {
                this.solr = solr;
            }

            public virtual void OnPostInsert(PostInsertEvent e) {
                if (e.Entity.GetType() != typeof (T))
                    return;
                solr.Add((T) e.Entity);
            }

            public virtual void OnPostDelete(PostDeleteEvent e) {
                if (e.Entity.GetType() != typeof (T))
                    return;
                solr.Delete((T) e.Entity);
            }

            public virtual void OnPostUpdate(PostUpdateEvent e) {
                if (e.Entity.GetType() != typeof (T))
                    return;
                solr.Add((T) e.Entity);
            }
        }

        private ISessionFactory sessionFactory;

        [Test]
        public void PostInsert() {
            using (var session = sessionFactory.OpenSession()) {
                session.Save(new Entity());
            }
        }

        [FixtureSetUp]
        public void FixtureSetup() {
            var nhConfig = new Configuration {
                Properties = new Dictionary<string, string> {
                    {Environment.ConnectionProvider, "NHibernate.Connection.DriverConnectionProvider"},
                    {Environment.ConnectionDriver, "NHibernate.Driver.SQLite20Driver"},
                    {Environment.Dialect, "NHibernate.Dialect.SQLiteDialect"},
                    {Environment.ConnectionString, "Data Source=test.db;Version=3;New=True;"},
                }
            };
            nhConfig.Register(typeof (Entity));
            var solr = MockRepository.GenerateMock<ISolrOperations<Entity>>();
            nhConfig.SetListener(ListenerType.PostInsert, new SolrNetListener<Entity>(solr));
            new SchemaExport(nhConfig).Execute(false, true, false, false);
            sessionFactory = nhConfig.BuildSessionFactory();
        }

        [FixtureTearDown]
        public void FixtureTeardown() {
            sessionFactory.Dispose();
        }
    }

    public class Entity {
        public virtual int Id { get; set; }
        public virtual string Description { get; set; }
        public virtual IList<string> Tags { get; set; }
    }
}