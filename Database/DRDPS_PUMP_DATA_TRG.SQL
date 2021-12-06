
CREATE OR REPLACE TRIGGER DRDPS_PUMP_DATA_TRG
AFTER INSERT OR UPDATE ON DR_DATAPOINT_STATE FOR EACH ROW
DECLARE

v_entityname VARCHAR2(85);

BEGIN
select name into v_entityname from entity where pkey = :NEW.entitykey;

if v_entityname like '%DNG%diiDS%-Pump%StopRun' then

        IF(:NEW.value is not null) THEN
            insert into PUMP_STATE_CHANGE(ENTITYKEY,UPDATETIME,VALUE)
                values(:NEW.entitykey, :NEW.updatetime, :NEW.value);

        END IF;

end if;
END;
/

