-- Publish as: Module:The Story of Red Cloud/NPC
local p = {}
local data = mw.loadData("Module:The Story of Red Cloud/NPCs/data")

local function arg(frame, name)
    return frame.args[name] or frame:getParent().args[name]
end

local function text(value)
    return mw.text.nowiki(tostring(value or ""))
end

local function percent(value)
    return string.format("%.3g%%", tonumber(value) or 0)
end

local function conditions(values)
    if not values or #values == 0 then
        return ""
    end
    return " <small>(" .. text(table.concat(values, "; ")) .. ")</small>"
end

function p.main(frame)
    local id = arg(frame, "id")
    local npc = data[id]
    if not npc then
        return "<strong class=\"error\">Unknown TSORC NPC ID: " .. text(id) .. "</strong>"
    end

    local lines = {
        "[[File:" .. npc.image .. "|220px|alt=" .. text(npc.wikiName) .. "]]",
        "{| class=\"wikitable\" style=\"float:right; clear:right; margin-left:1em; min-width:18em\"",
        "|+ " .. text(npc.wikiName),
        "|-\n! Category\n| " .. text(npc.category),
        "|-\n! Health\n| " .. text(npc.lifeMax),
        "|-\n! Defense\n| " .. text(npc.defense),
        "|-\n! Contact damage\n| " .. text(npc.damage),
        "|-\n! Knockback resistance\n| " .. string.format("%.0f%%", (tonumber(npc.knockbackResist) or 0) * 100),
        "|}",
    }

    if npc.loot and #npc.loot > 0 then
        table.insert(lines, "== Loot ==")
        for _, drop in ipairs(npc.loot) do
            local stack = drop.minStack == drop.maxStack and drop.minStack or drop.minStack .. "–" .. drop.maxStack
            table.insert(lines, "* " .. text(drop.itemName) .. " ×" .. stack .. " — " .. percent(drop.chancePercent) .. conditions(drop.conditions))
        end
    end

    return frame:preprocess(table.concat(lines, "\n"))
end

return p
