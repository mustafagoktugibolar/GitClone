using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using GitClone.Interfaces;
using GitClone.Models;
using GitClone.Services;
using Xunit;

namespace GitClone.Tests
{
    /// <summary>
    /// A fake hash service that just returns a known, deterministic string.
    /// </summary>
    internal class FakeHashService : IHashService
    {
        public string ComputeSha1(string content)
        {
            throw new NotImplementedException();
        }

        public string ComputeSha256(string input)
        {
            // We don’t care about real hashing here; just return something unique:
            return "HASHED(" + input + ")";
        }
    }

    public class ConfigServiceTests : IDisposable
    {
        private readonly string _tempHome;
        private readonly string _tempCurrentDir;

        public ConfigServiceTests()
        {
            // 1) Create an isolated temp directory for “HOME” (where Global config lives).
            //    On Windows, Environment.SpecialFolder.UserProfile uses %USERPROFILE%.
            //    On Linux/Mac, it uses $HOME.
            _tempHome = Path.Combine(Path.GetTempPath(), "GitCloneTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempHome);

            // Override the HOME/USERPROFILE environment variable for the duration of these tests:
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Environment.SetEnvironmentVariable("USERPROFILE", _tempHome);
            }
            else
            {
                Environment.SetEnvironmentVariable("HOME", _tempHome);
            }

            // 2) Create another isolated temp directory for CurrentDirectory (where Local config will go).
            _tempCurrentDir = Path.Combine(Path.GetTempPath(), "GitCloneTestsCurrent", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempCurrentDir);
            Directory.SetCurrentDirectory(_tempCurrentDir);
        }

        public void Dispose()
        {
            // Clean up: restore environment and delete the temp folders
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Environment.SetEnvironmentVariable("USERPROFILE", null);
            else
                Environment.SetEnvironmentVariable("HOME", null);

            // Restore the original working directory so other tests aren’t confused
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            // Delete the temp directories
            try { Directory.Delete(_tempHome, recursive: true); } catch { }
            try { Directory.Delete(_tempCurrentDir, recursive: true); } catch { }
        }

        private ConfigService CreateService()
        {
            // Always inject our fake hash service
            return new ConfigService(hashService: new FakeHashService());
        }

        [Fact]
        public void EnsureCreated_CreatesGlobalAndLocalDirectoriesAndFiles()
        {
            // Arrange
            var service = CreateService();

            // Act
            // At this point, neither global nor local config dirs exist.
            service.EnsureCreated();

            // Assert:
            // 1. Global config directory should now exist under %USERPROFILE%/.ilos/
            var globalDir = Path.Combine(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? Environment.GetEnvironmentVariable("USERPROFILE")!
                    : Environment.GetEnvironmentVariable("HOME")!,
                ".ilos");
            Assert.True(Directory.Exists(globalDir), "Global config directory was not created.");

            // 2. The global config file itself should exist, since EnsureCreated() calls CreateGlobalConfigFile() if missing.
            var globalConfigFile = Path.Combine(globalDir, "config.json");
            Assert.True(File.Exists(globalConfigFile), "Global config file was not created.");

            // 3. Local config directory should now exist under <CurrentDirectory>/.ilos/configs/
            var localDir = Path.Combine(_tempCurrentDir, ".ilos", "configs");
            Assert.True(Directory.Exists(localDir), "Local config directory was not created.");

            // 4. The local config file should exist after calling EnsureCreated (because it calls InitLocalConfig, which writes local JSON).
            var localConfigFile = Path.Combine(localDir, "config.json");
            Assert.True(File.Exists(localConfigFile), "Local config file was not created.");

            // 5. The content of the global config file should at least parse as JSON:
            string globalJson = File.ReadAllText(globalConfigFile);
            Assert.False(string.IsNullOrWhiteSpace(globalJson));
            var parsedGlobal = JsonSerializer.Deserialize<Config>(globalJson);
            Assert.NotNull(parsedGlobal);

            // 6. The content of the local config file should also parse as JSON:
            string localJson = File.ReadAllText(localConfigFile);
            Assert.False(string.IsNullOrWhiteSpace(localJson));
            var parsedLocal = JsonSerializer.Deserialize<Config>(localJson);
            Assert.NotNull(parsedLocal);
        }

        [Fact]
        public void AddGlobalConfig_WhenGlobalHasNoUsers_AddsNewUserAndSetsActive_WhenUserEntersY()
        {
            // Arrange
            var service = CreateService();

            // 1) Prepare a minimal “empty” global config file so AddGlobalConfig can load & modify it.
            var globalDir = Path.Combine(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? Environment.GetEnvironmentVariable("USERPROFILE")!
                    : Environment.GetEnvironmentVariable("HOME")!,
                ".ilos");
            Directory.CreateDirectory(globalDir);
            var globalConfigFile = Path.Combine(globalDir, "config.json");

            // Write "{}" so Deserialize<Config> yields a Config with default (empty) lists.
            File.WriteAllText(globalConfigFile, "{}");

            // 2) Redirect Console.In so that when AddGlobalConfig asks "Do you want to make the user active? (Y/N):"
            //    we automatically feed it "y\n".
            var input = new StringReader("y\n");
            Console.SetIn(input);

            // Act
            service.AddGlobalConfig(username: "alice", email: "alice@example.com", password: "password123");

            // Assert:
            // Re‐read the global config file from disk:
            string updatedJson = File.ReadAllText(globalConfigFile);
            var updatedCfg = JsonSerializer.Deserialize<Config>(updatedJson);
            Assert.NotNull(updatedCfg);

            // 1) There should be exactly one user in Configs
            Assert.Single(updatedCfg.Configs);

            var user = updatedCfg.Configs.First();
            Assert.Equal("alice",               user.Username);
            Assert.Equal("alice@example.com",   user.Mail);
            Assert.Equal("HASHED(password123)", user.PasswordHash);

            // 2) Because we typed “y”, ActiveUser should have been set to user.Mail
            Assert.Equal("alice@example.com", updatedCfg.ActiveUser);
        }

        [Fact]
        public void AddGlobalConfig_WhenEmailAlreadyExists_DoesNotAddDuplicate()
        {
            // Arrange
            var service = CreateService();

            // Create a global config with one existing user “bob”
            var globalDir = Path.Combine(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? Environment.GetEnvironmentVariable("USERPROFILE")!
                    : Environment.GetEnvironmentVariable("HOME")!,
                ".ilos");
            Directory.CreateDirectory(globalDir);
            var globalConfigFile = Path.Combine(globalDir, "config.json");

            var existing = new Config
            {
                ActiveUser = "bob@doe.com",
                Configs = new List<User>
                {
                    new User
                    {
                        Username     = "bob",
                        Mail         = "bob@doe.com",
                        PasswordHash = "somehash"
                    }
                }
            };
            File.WriteAllText(globalConfigFile, JsonSerializer.Serialize(existing, new JsonSerializerOptions { WriteIndented = true }));

            // Redirect Console.In so we never block (user prompt won’t matter, because we shouldn’t get there).
            Console.SetIn(new StringReader("n\n"));

            // Act
            service.AddGlobalConfig(username: "bob", email: "bob@doe.com", password: "newpass");

            // Assert:
            // Since “bob@doe.com” already existed, no new user should have been appended.
            string afterJson = File.ReadAllText(globalConfigFile);
            var afterCfg = JsonSerializer.Deserialize<Config>(afterJson)!;

            Assert.Single(afterCfg.Configs);
            Assert.Equal("bob@doe.com", afterCfg.Configs[0].Mail);
            Assert.Equal("bob",           afterCfg.Configs[0].Username);

            // And ActiveUser should remain the same as before
            Assert.Equal("bob@doe.com", afterCfg.ActiveUser);
        }

        [Fact]
        public void InitLocalConfig_WhenGlobalHasActiveUser_CreatesLocalWithOnlyThatUser()
        {
            // Arrange
            var service = CreateService();

            // Create a global config file that contains two users but one marked “active.”
            var globalDir = Path.Combine(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? Environment.GetEnvironmentVariable("USERPROFILE")!
                    : Environment.GetEnvironmentVariable("HOME")!,
                ".ilos");
            Directory.CreateDirectory(globalDir);
            var globalConfigFile = Path.Combine(globalDir, "config.json");

            var user1 = new User { Username = "u1", Mail = "u1@x.com", PasswordHash = "h1" };
            var user2 = new User { Username = "u2", Mail = "u2@x.com", PasswordHash = "h2" };
            var seedConfig = new Config
            {
                ActiveUser = user2.Mail,
                Configs    = new List<User> { user1, user2 }
            };
            File.WriteAllText(globalConfigFile,
                             JsonSerializer.Serialize(seedConfig, new JsonSerializerOptions { WriteIndented = true }));

            // Act
            service.InitLocalConfig();

            // Assert:
            // The local config is written under <CurrentDirectory>/.ilos/configs/config.json
            var localConfigFile = Path.Combine(_tempCurrentDir, ".ilos", "configs", "config.json");
            Assert.True(File.Exists(localConfigFile), "Local config file was not created.");

            var localJson = File.ReadAllText(localConfigFile);
            var localCfg = JsonSerializer.Deserialize<Config>(localJson)!;

            // It should contain exactly one user (the “active” one, user2).
            Assert.Single(localCfg.Configs);
            Assert.Equal("u2@x.com", localCfg.Configs[0].Mail);
            Assert.Equal("u2@x.com", localCfg.ActiveUser);
        }
    }
}
