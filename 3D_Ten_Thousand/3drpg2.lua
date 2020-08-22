--[[
package.path = './deps/?.lua;./deps/socket/?.lua;'..package.path
package.cpath = './deps/?.dll;./deps/socket/?.dll;'..package.cpath
package.cpath = './deps/?.so;./deps/socket/?.so;'..package.cpath

require 'socket'
]]--
local json = require 'dkjson'
--local debuggee = require 'vscode-debuggee'
--local startResult, breakerType = debuggee.start(json)
--print('debuggee start ->', startResult, breakerType)
frame_rate = 1/2;
log_level = 0;
speed = 5;

--Delete elements in table
local function removeElementByKey(tbl,key)
  --Create a temporary table
  local tmp ={}
  for i in pairs(tbl) do
      table.insert(tmp,i)
  end

  local newTbl = {}
  --Use the while loop to weed out unwanted elements
  local i = 1
  while i <= #tmp do
      local val = tmp [i]
      if val == key then
          --Remove if it needs to be removed
          table.remove(tmp,i)
       else
          --If it's not culled, put it in a new tab
          newTbl[val] = tbl[val]
          i = i + 1
       end
   end
  return newTbl
end

function length(x, z)
  return math.sqrt(x*x + z*z);
end

function normalize(x, z)
return x/length(x, z), z/length(x, z)
end

function location(x, z, dx, dz, time)

  local step = speed * (pelagia.MS() - time) / 1000;
  local nx, nz = normalize(dx - x, dz - z);
  local lx = x + nx * step;
  local lz = z + nz * step;

  if length(nx * step, nz * step) >= length(dx- x,dz - z) then
    return dx, dz;
  else 
    return lx, lz;
  end
end

function space(param)
  pelagia.Elog(log_level, "space----"..param);
  local dvalue = json.decode(param);

  if (dvalue.cmd == "init") then
    
    local orderid = pelagia.CreateOrderID();

    _G[orderid] = {};
    _G[orderid]["globle"] = {};
    _G[orderid]["cell"] = {};
    _G[orderid]["coordinate"] = {};
    _G[orderid]["globle"]["global_x"] = dvalue.global_x;
    _G[orderid]["globle"]["global_z"] = dvalue.global_z;

    local space_Arg = {};
    space_Arg.cmd = "space_ret";
    space_Arg.id = orderid;
    local json_arg = json.encode(space_Arg);
    pelagia.RemoteCall("manager", json_arg);

    --Triggers a tick at the speed of a given frame
    local tick_Arg = {};
    tick_Arg.cmd = "tick";
    tick_Arg.id = orderid;
    local json_arg = json.encode(tick_Arg);
    pelagia.TimerWithOrderID(orderid, frame_rate,"space", json_arg);

  elseif (dvalue.cmd == "write") then

    local orderid = pelagia.OrderID();
    --new
    local xcell = math.floor(dvalue.x / 10);
    local zcell = math.floor(dvalue.z / 10);
    _G[orderid]["cell"]["x"..xcell.."z"..zcell] = {};
    _G[orderid]["cell"]["x"..xcell.."z"..zcell][dvalue.name] = 1;

    _G[orderid]["coordinate"][dvalue.name] = {};
    _G[orderid]["coordinate"][dvalue.name]["x"] = dvalue.x;
    _G[orderid]["coordinate"][dvalue.name]["z"] = dvalue.z;
    _G[orderid]["coordinate"][dvalue.name]["time"] = dvalue.time;
    _G[orderid]["coordinate"][dvalue.name]["dx"] = dvalue.dx;
    _G[orderid]["coordinate"][dvalue.name]["dz"] = dvalue.dz;

  elseif (dvalue.cmd == "read") then
    local orderid = pelagia.OrderID();

    local xcell = math.floor(dvalue.x / 10);
    local zcell = math.floor(dvalue.z / 10);

    local space_Arg = {};
    space_Arg.ret = {};
    for i = -1, 1, 1 do
      for j = -1, 1, 1 do
          local xncell = xcell + i;
          local zncell = zcell + j;

          if xncell >= 0 and zncell >= 0  and xncell <= _G[orderid]["globle"]["global_x"]/10  and zncell <= _G[orderid]["globle"]["global_z"]/10 then
            local scell = "x"..xncell.."z"..zncell
            if _G[orderid]["cell"][scell] ~= nil then
              for key, value in pairs(_G[orderid]["cell"][scell]) do
                if key ~= dvalue.name then
                  space_Arg.ret[key] = value;
                end
              end
            end
          end
      end 
    end
   
    space_Arg.cmd = "space_ret";
    space_Arg.orderid = orderid;
    space_Arg.name = dvalue.name;
    local json_arg = json.encode(space_Arg);
    pelagia.RemoteCallWithOrderID(dvalue.orderid, "role", json_arg);

  elseif (dvalue.cmd == "tick") then

    local orderid = pelagia.OrderID();
    for key, value in pairs(_G[orderid]["coordinate"]) do
      --new
      local x, z = location(value["x"], value["z"]
      , value["dx"], value["dz"], value["time"]);

      pelagia.Elog(log_level, "space----"..key..":x:"..x..":z:"..z);

      local xcell = math.floor(x / 10);
      local zcell = math.floor(z / 10);
      local scell = "x"..xcell.."z"..zcell;

      if value["cell"] ~= scell then
        if _G[orderid]["cell"][value["cell"]] ~= nil then
            removeElementByKey(_G[orderid]["cell"][value["cell"]], key);
        end
        
        if _G[orderid]["cell"][scell] == nil then
            _G[orderid]["cell"][scell] = {};
        end
        _G[orderid]["cell"][scell][key] = 1;
        value["cell"] = scell;
      end
    end

    --Triggers a tick at the speed of a given frame
    local tick_Arg = {};
    tick_Arg.cmd = "tick";
    tick_Arg.id = orderid;
    local json_arg = json.encode(tick_Arg);
    pelagia.TimerWithOrderID(orderid, frame_rate,"space", json_arg);

  end
  return 1;
end

function roleProc(cmd, orderid, rolename, value, dvalue)
  pelagia.Elog(log_level, "roleProc----");

  if (cmd == "create") then
    local spacekey;
    local limite = math.random(1, _G[orderid]["space_count"]);

    for key, value in pairs(_G[orderid]["space"]) do
      spacekey = key;
      limite = limite - 1;
      if limite == 0 then
          break;
      end
    end

    value["in_space"] = spacekey;
    value["x"] = dvalue.x;
    value["z"] = dvalue.z;
    value["dx"] = math.random(1, 100);
    value["dz"] = math.random(1, 100);
    value["space_player"] = {};
    value["tick_time"] = pelagia.MS();
    value["time"] = value["tick_time"] ;
    value["name"] = rolename;

    local space_Arg = {};
    space_Arg.cmd = "regist";
    space_Arg.playerid = orderid;
    space_Arg.x = value["x"];
    space_Arg.z = value["z"];
    space_Arg.dx = value["dx"];
    space_Arg.dz = value["dz"];
    space_Arg.time = value["time"];
    space_Arg.name = rolename;
    local json_arg = json.encode(space_Arg);
    pelagia.RemoteCallWithOrderID(spacekey, "space", json_arg);

    --Send message to view
    local ret_player_arg = {};
    ret_player_arg.cmd = "move";
    ret_player_arg.name = rolename;
    ret_player_arg.d_x = value["dx"];
    ret_player_arg.d_z = value["dz"];
    local json_arg = json.encode(ret_player_arg);
    pelagia.EventSend(_G[orderid]["event"], json_arg);

  elseif (cmd == "tick") then
    --Calculate current position
    local x, z = location(value["x"], value["z"]
      , value["dx"], value["dz"], value["time"]);

    pelagia.Elog(log_level, "roleProc----"..rolename..":x:"..x..":z:"..z);
    if x == value["dx"] and z ==value["dz"] then
      --Reset goals
      value["x"] = value["dx"];
      value["z"] = value["dz"];

      value["dx"] = math.random(1, 100);
      value["dz"] = math.random(1, 100);

      value["time"] = pelagia.MS();

      --Send message to view
      local ret_player_arg = {};
      ret_player_arg.cmd = "move";
      ret_player_arg.name = rolename;
      ret_player_arg.d_x = value["dx"];
      ret_player_arg.d_z = value["dz"];
      local json_arg = json.encode(ret_player_arg);
      pelagia.EventSend(_G[orderid]["event"], json_arg);

      --Send message to space
      local space_Arg = {};
      space_Arg.cmd = "write";
      space_Arg.name = rolename;
      space_Arg.orderid = orderid;
      space_Arg.x = value["x"];
      space_Arg.z = value["z"];
      space_Arg.dx = value["dx"];
      space_Arg.dz = value["dz"];
      space_Arg.time = value["time"];
      local json_arg = json.encode(space_Arg);
      pelagia.RemoteCallWithOrderID(value["in_space"], "space", json_arg);
    end

    --Randomly launch queries around players for logical processing
    if math.random(1, 100) == 0 then
      local space_Arg = {};
      space_Arg.cmd = "read";
      space_Arg.name = rolename;
      space_Arg.orderid = orderid;
      space_Arg.x = x;
      space_Arg.z = z;
      local json_arg = json.encode(space_Arg);
      pelagia.RemoteCallWithOrderID(value["in_space"], "space", json_arg);     
    end
  end
end

function role(param)
  pelagia.Elog(log_level, "role----"..param);
  local dvalue = json.decode(param);

  if (dvalue.cmd == "init") then
    local orderid = pelagia.CreateOrderID();
    _G[orderid] = {};
    _G[orderid]["role"] = {};
    
    local space_Arg = {};
    space_Arg.cmd = "role_ret";
    space_Arg.id = orderid;
    local json_arg = json.encode(space_Arg);
    pelagia.RemoteCall("manager", json_arg);

    --Triggers a tick at the speed of a given frame
    local tick_Arg = {};
    tick_Arg.cmd = "tick";
    tick_Arg.id = orderid;
    local json_arg = json.encode(tick_Arg);
    pelagia.TimerWithOrderID(orderid, frame_rate,"role", json_arg);

  elseif (dvalue.cmd == "create") then

    local orderid = pelagia.OrderID();
    _G[orderid]["role"][dvalue.name] = {};
    _G[orderid]["space"] = dvalue.space;
    _G[orderid]["space_count"] = dvalue.space_count;
    _G[orderid]["event"] = dvalue.event;
    
    roleProc(dvalue.cmd, orderid, dvalue.name, _G[orderid]["role"][dvalue.name], dvalue);

  elseif (dvalue.cmd == "tick") then
    local orderid = pelagia.OrderID();

    for key, value in pairs(_G[orderid]["role"]) do
      roleProc(dvalue.cmd, orderid, key, value, dvalue);
    end

    --Triggers a tick at the speed of a given frame
    local tick_Arg = {};
    tick_Arg.cmd = "tick";
    tick_Arg.id = orderid;
    local json_arg = json.encode(tick_Arg);
    pelagia.TimerWithOrderID(orderid, frame_rate,"role", json_arg);

  elseif (dvalue.cmd == "space_ret") then
    local orderid = pelagia.OrderID();
    _G[orderid]["role"][dvalue.name]["space_player"][dvalue.orderid] = dvalue.ret;
  end
end

function manager(param)
  pelagia.Elog(log_level, "init----"..param);
  local dvalue = json.decode(param);

  if (dvalue.cmd == "init") then

    --create space server
      pelagia.Set2("global", "x", dvalue.global_x);
      pelagia.Set2("global", "z", dvalue.global_z);
      pelagia.Set2("global_str", "event", dvalue.event);
      pelagia.Set2("global", "space_ret", 0);
      pelagia.Set2("global", "role_ret", 0);
      pelagia.Del2("server", "space");
      pelagia.Del2("server", "role");

      local space_Arg = {};
      space_Arg.cmd = "init";
      space_Arg.global_x = dvalue.global_x
      space_Arg.global_z = dvalue.global_z
      local json_arg = json.encode(space_Arg);
      local ret = pelagia.RemoteCallWithMaxCore("space", json_arg);
      pelagia.Set2("global", "space_count", ret);

  elseif (dvalue.cmd == "space_ret") then

      local count = pelagia.Get2("global", "space_count");
      local ret = pelagia.Get2("global", "space_ret");
      ret = ret + 1;
      pelagia.Set2("global", "space_ret", ret);
      pelagia.SetAdd2("server", "space", dvalue.id);

      pelagia.Elog(log_level, "space_ret::"..dvalue.id.."::"..ret);
      if ret == count then

        --create role server
        local space_Arg = {};
        space_Arg.cmd = "init";
        space_Arg.global_x = dvalue.global_x
        space_Arg.global_z = dvalue.global_z
        local json_arg = json.encode(space_Arg);
        local ret = pelagia.RemoteCallWithMaxCore("role", json_arg);
        pelagia.Set2("global", "role_count", ret);
      end

  elseif (dvalue.cmd == "role_ret") then

    local count = pelagia.Get2("global", "role_count");
    local ret = pelagia.Get2("global", "role_ret");
    ret = ret + 1;
    pelagia.Set2("global", "role_ret", ret);
    pelagia.SetAdd2("server", "role", dvalue.id);

    pelagia.Elog(log_level, "role_ret::"..dvalue.id.."::"..ret);
    if ret == count then

      local event = pelagia.Get2("global_str", "event");
      --init complete
      local ret_player_arg = {};
      ret_player_arg.cmd = "init";
      local json_arg = json.encode(ret_player_arg);
      pelagia.EventSend(event, json_arg);
    end

  elseif (dvalue.cmd == "create") then

    --Start the creation of role
    local event = pelagia.Get2("global_str", "event");
    local role_count = pelagia.Get2("global", "role_count");
    local space_count = pelagia.Get2("global", "space_count");

    --Because roles use space frequently, they are distributed to each role
    local space_server = pelagia.SetMembers2("server", "space");
    local role_server = pelagia.SetMembers2("server", "role");

    --Randomly assigned to a role server
    local rolekey;
    local limite = math.random(1, role_count);

    for key, value in pairs(role_server) do
      rolekey = key;
      limite = limite - 1;
      if limite == 0 then
          break;
      end
    end

    local player_Arg = {};
    player_Arg.cmd = "create";
    player_Arg.name = dvalue.name;
    player_Arg.x = dvalue.x;
    player_Arg.z = dvalue.z;
    player_Arg.space = space_server;
    player_Arg.space_count = space_count;
    player_Arg.event = event;
    local json_arg = json.encode(player_Arg);
    pelagia.RemoteCallWithOrderID(rolekey, "role", json_arg);
  end

  return 1;
end

function test(param)
  pelagia.Elog(log_level, "base param:".. param)
  local dvalue = json.decode(param)

  local manger_Arg = {};
  manger_Arg.cmd = "init";
  manger_Arg.global_x = 100;
  manger_Arg.global_z = 100;
  manger_Arg.event = dvalue.event;
  local json_arg = json.encode(manger_Arg);
  pelagia.RemoteCall("manager", json_arg)

  return 1;
end

function test2(param)
  pelagia.Elog(log_level, "base param:".. param)
  local dvalue = json.decode(param)

  for i = 1, 10000, 1 do
    local manger_Arg = {};
    manger_Arg.cmd = "create";
    manger_Arg.name = "role"..i;
    manger_Arg.x = math.random(1, 100);
    manger_Arg.z = math.random(1, 100);
    manger_Arg.event = dvalue.event;
    local json_arg = json.encode(manger_Arg);
    pelagia.RemoteCall("manager", json_arg)
  end
  return 1;
end

